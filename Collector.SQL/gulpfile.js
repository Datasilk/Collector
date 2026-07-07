'use strict';

const { series } = require('gulp');
const fs = require('fs');
const path = require('path');

const ROOT = __dirname;
const SOURCE_ROOT = path.join(ROOT, 'dbo');
const TARGET_ROOT = path.join(ROOT, 'postgresql');
const SEQUENCE_FOLDER_NAME = 'Sequences';
const COMPILED_FILENAME = 'postgresql.sql';
const COMPILED_OUTPUT = path.join(TARGET_ROOT, COMPILED_FILENAME);

let cachedSequences;

const typeReplacements = [
  { regex: /NVARCHAR\(MAX\)/gi, value: 'TEXT' },
  { regex: /NVARCHAR\((\d+)\)/gi, value: 'VARCHAR($1)' },
  { regex: /NCHAR\((\d+)\)/gi, value: 'CHAR($1)' },
  { regex: /NCHAR\b/gi, value: 'CHAR' },
  { regex: /"NVARCHAR"\(MAX\)/gi, value: 'TEXT' },
  { regex: /"NVARCHAR"\((\d+)\)/gi, value: 'VARCHAR($1)' },
  { regex: /"NCHAR"\((\d+)\)/gi, value: 'CHAR($1)' },
  { regex: /"NCHAR"\b/gi, value: 'CHAR' },
  { regex: /"UUID"/gi, value: 'UUID' },
  { regex: /"BOOLEAN"/gi, value: 'BOOLEAN' },
  { regex: /"TIMESTAMPTZ"/gi, value: 'TIMESTAMPTZ' },
  { regex: /"TIMESTAMP"/gi, value: 'TIMESTAMP' },
  { regex: /"INT"/gi, value: 'INT' },
  { regex: /\bINT\b/gi, value: 'INT' },
  { regex: /"BIGINT"/gi, value: 'BIGINT' },
  { regex: /"SMALLINT"/gi, value: 'SMALLINT' },
  { regex: /"DOUBLE\s+PRECISION"/gi, value: 'DOUBLE PRECISION' },
  { regex: /"NUMERIC"\(([^)]+)\)/gi, value: 'NUMERIC($1)' },
  { regex: /"NUMERIC"/gi, value: 'NUMERIC' },
  { regex: /"TEXT"/gi, value: 'TEXT' },
  { regex: /UNIQUEIDENTIFIER/gi, value: 'UUID' },
  { regex: /DATETIME2\(7\)/gi, value: 'TIMESTAMP(6)' },
  { regex: /DATETIME2/gi, value: 'TIMESTAMPTZ' },
  { regex: /SMALLDATETIME/gi, value: 'TIMESTAMP' },
  { regex: /DATETIME/gi, value: 'TIMESTAMP' },
  { regex: /BIT/gi, value: 'BOOLEAN' },
  { regex: /FLOAT/gi, value: 'DOUBLE PRECISION' },
  { regex: /SMALLMONEY/gi, value: 'NUMERIC(10,4)' },
  { regex: /MONEY/gi, value: 'NUMERIC(19,4)' },
  { regex: /TINYINT/gi, value: 'SMALLINT' }
];

function formatPrimaryKeyClause(clause) {
  const keyMatch = clause.match(/^KEY\s*\(([^)]+)\)(.*)$/i);

  if (!keyMatch) {
    return 'PRIMARY KEY';
  }

  const columnsPart = keyMatch[1];
  const suffix = keyMatch[2].trim();

  const columns = columnsPart
    .split(',')
    .map((col) => col.trim())
    .filter(Boolean)
    .map((col) => col.replace(/"([^"]+)"/g, '$1'))
    .map((col) => `"${col}"`);

  const suffixUpper = suffix ? ` ${suffix.replace(/[a-z]+/gi, (match) => match.toUpperCase())}` : '';

  return `PRIMARY KEY (${columns.join(', ')})${suffixUpper}`;
}

const nonPortableStatements = [
  /SET\s+ANSI_NULLS\s+(ON|OFF);?/gi,
  /SET\s+QUOTED_IDENTIFIER\s+(ON|OFF);?/gi,
  /SET\s+NOCOUNT\s+ON;?/gi,
  /ON\s+\[PRIMARY\]/gi,
  /TEXTIMAGE_ON\s+\[PRIMARY\]/gi,
  /WITH\s*\([^)]+\)/gi,
  /USE\s+\[[^\]]+\];?/gi
];

const overrideGenerators = {};

function ensureCreateGuards(sql) {
  let output = sql;

  output = output.replace(/CREATE\s+TABLE\s+(?!IF\s+NOT\s+EXISTS)/gi, 'CREATE TABLE IF NOT EXISTS ');
  output = output.replace(/CREATE\s+(UNIQUE\s+)?INDEX\s+(?!IF\s+NOT\s+EXISTS)/gi, (match) => `${match}IF NOT EXISTS `);
  output = output.replace(/CREATE\s+VIEW\s+(?!IF\s+NOT\s+EXISTS)/gi, 'CREATE VIEW IF NOT EXISTS ');
  output = output.replace(/CREATE\s+FUNCTION\s+/gi, 'CREATE OR REPLACE FUNCTION ');
  output = output.replace(/CREATE\s+PROCEDURE\s+/gi, 'CREATE OR REPLACE PROCEDURE ');

  return output;
}

function ensureStatementTerminators(sql) {
  let output = sql;

  // Ensure CREATE VIEW lines terminate properly
  output = output.replace(/(\s*CREATE\s+VIEW[^\n]*)(\n)/gi, (line, leading, statement) => {
    const trimmed = statement.trimEnd();
    if (trimmed.endsWith(';')) {
      return line;
    }
    return `${leading}${trimmed};\n`;
  });

  // Remove trailing commas before closing parenthesis or constraints while preserving whitespace
  output = output.replace(/,\s*(?=CONSTRAINT)/gi, '\n    ');
  output = output.replace(/,\s*(?=\))/g, '\n');

  // Append semicolons to the standalone closing parenthesis line for tables
  output = output.replace(/^(\s*\))(\s*)$/gm, (line, closingParen, whitespace) => {
    const trimmed = line.trim();
    if (trimmed.endsWith(');')) {
      return line;
    }
    return `${closingParen};${whitespace}`;
  });

  return output;
}

function normalizeIndexStatements(sql) {
  const pattern = /(CREATE\s+(?:UNIQUE\s+)?INDEX[\s\S]+?)(?=(?:\n\s*\n)|(?:\n--)|$)/gi;

  return sql.replace(pattern, (block) => {
    const lines = block.split('\n');
    if (!lines.length) {
      return block;
    }

    const firstLine = lines[0].trimEnd();
    const rest = lines
      .slice(1)
      .map((line) => line.trim())
      .filter((line) => line.length > 0)
      .map((line) => `    ${line}`);

    const rebuilt = [firstLine, ...rest].join('\n');
    const trailingMatch = block.match(/\s*$/);
    const trailing = trailingMatch ? trailingMatch[0] : '\n';

    return `${rebuilt}${trailing}`;
  });
}

function finalizeIndexBlocks(sql) {
  const pattern = /(CREATE\s+(?:UNIQUE\s+)?INDEX[\s\S]+?)(?=(?:\n\s*\n)|(?:\n--)|$)/gi;

  return sql.replace(pattern, (block) => {
    let processed = block.replace(/\s+ON\s+(?:public\.)?["']?([A-Za-z0-9_]+)["']?\s*\(([^)]+)\)/gi, (match, tableName, columnList) => {
      const columns = columnList
        .split(',')
        .map(col => col.trim())
        .filter(Boolean)
        .map(col => {
          const parts = col.split(/\s+/);
          const colName = parts[0].replace(/^["']|["']$/g, '');
          const rest = parts.slice(1).join(' ');
          return rest ? `"${colName}" ${rest}` : `"${colName}"`;
        })
        .join(', ');
      
      return ` ON public."${tableName}" (${columns})`;
    });

    const trailingMatch = processed.match(/\s*$/);
    const trailing = trailingMatch ? trailingMatch[0] : '';
    const core = processed.slice(0, processed.length - trailing.length).trimEnd();

    if (core.endsWith(';')) {
      return processed;
    }

    return `${core};${trailing}`;
  });
}

function normalizeProcedureParameters(sql) {
  const pattern = /(CREATE\s+(?:OR\s+REPLACE\s+)?PROCEDURE\s+[^\n]+)([\s\S]*?)(\bAS\b)/gi;

  return sql.replace(pattern, (match, header, paramBlock, asKeyword) => {
    const rawLines = paramBlock
      .split('\n')
      .map((line) => line.trim())
      .filter((line) => line.length > 0);

    const paramLines = rawLines.filter((line) => line.startsWith('@'));

    if (!paramLines.length) {
      return match;
    }

    const parsed = paramLines.map((line) => {
      const hasComma = /,\s*$/.test(line);
      const cleanLine = line.replace(/,\s*$/, '');
      const paramMatch = cleanLine.match(/^@([A-Za-z0-9_]+)\s+([^=\s]+(?:\s*\([^)]*\))?)(?:\s*=\s*(.+))?$/i);

      if (!paramMatch) {
        return { raw: cleanLine.replace(/^@/, ''), hasComma };
      }

      const [, name, type, defaultValue] = paramMatch;

      return {
        name,
        type: type.trim(),
        defaultValue: defaultValue ? defaultValue.trim() : null,
        hasComma
      };
    });

    const formatted = parsed
      .filter((param) => param.name || param.raw)
      .map((param, index, array) => {
        if (!param.name) {
          const comma = index < array.length - 1 ? ',' : '';
          return `    ${param.raw}${comma}`;
        }

        const defaultClause = param.defaultValue ? ` DEFAULT ${param.defaultValue}` : '';
        const comma = index < array.length - 1 ? ',' : '';
        return `    IN ${param.name} ${param.type}${defaultClause}${comma}`;
      });

    if (!formatted.length) {
      return match;
    }

    const normalized = `\n(\n${formatted.join('\n')}\n)\n`;

    const normalizedHeader = header.replace(/CREATE\s+PROCEDURE/i, 'CREATE OR REPLACE PROCEDURE ');

    return `${normalizedHeader}${normalized}${asKeyword}`;
  });
}

function stripTsqlVariables(sql) {
  return sql.replace(/(^|[\s,()=+\-*/])@([A-Za-z0-9_]+)/g, '$1$2');
}

function sanitizeTableConstraints(sql) {
  return sql.replace(/^(.*CONSTRAINT.*)$/gim, (line) =>
    line.replace(/\s+(ASC|DESC)(?=\s*[),])/gi, '')
  );
}

function removeTrailingTableCommas(sql) {
  return sql.replace(/,\s*(\))/g, '\n$1');
}

function normalizeBooleanDefaults(sql) {
  return sql.replace(/(BOOLEAN[^,\n]*?DEFAULT\s+)(0|1)/gi, (match, prefix, value) => {
    const normalized = value === '1' ? 'TRUE' : 'FALSE';
    return `${prefix}${normalized}`;
  });
}

function extractTableConstraints(sql) {
  const tableRegex = /CREATE\s+TABLE\s+IF\s+NOT\s+EXISTS\s+[^\s(]+\s*\(/gi;
  let result = '';
  let lastIndex = 0;
  let match;

  while ((match = tableRegex.exec(sql)) !== null) {
    const headerStart = match.index;
    const openParenIndex = sql.indexOf('(', headerStart);

    if (openParenIndex === -1) {
      continue;
    }

    const closingParenIndex = findClosingParenthesis(sql, openParenIndex);

    if (closingParenIndex === -1) {
      continue;
    }

    let blockEnd = closingParenIndex + 1;
    while (blockEnd < sql.length && /\s/.test(sql[blockEnd])) {
      blockEnd++;
    }
    if (sql[blockEnd] === ';') {
      blockEnd++;
    }

    const tableBlock = sql.slice(headerStart, blockEnd);
    const processedBlock = transformTableBlock(tableBlock);

    result += sql.slice(lastIndex, headerStart);
    result += processedBlock;
    lastIndex = blockEnd;
  }

  result += sql.slice(lastIndex);
  return result;
}

function findClosingParenthesis(content, openIndex) {
  let depth = 0;
  let inSingleQuote = false;
  let inDoubleQuote = false;

  for (let i = openIndex; i < content.length; i += 1) {
    const char = content[i];

    if (char === "'" && !inDoubleQuote) {
      const isEscaped = content[i - 1] === '\\';
      if (!isEscaped) {
        inSingleQuote = !inSingleQuote;
      }
    } else if (char === '"' && !inSingleQuote) {
      const isEscaped = content[i - 1] === '\\';
      if (!isEscaped) {
        inDoubleQuote = !inDoubleQuote;
      }
    }

    if (inSingleQuote || inDoubleQuote) {
      continue;
    }

    if (char === '(') {
      depth += 1;
    } else if (char === ')') {
      depth -= 1;
      if (depth === 0) {
        return i;
      }
    }
  }

  return -1;
}

function transformTableBlock(block) {
  const headerMatch = block.match(/^(CREATE\s+TABLE\s+IF\s+NOT\s+EXISTS\s+[^\s(]+)/i);
  if (!headerMatch) {
    return block;
  }

  const tableName = headerMatch[1].split(/\s+/).pop();
  const openParenIndex = block.indexOf('(');
  const closeParenIndex = block.lastIndexOf(')');

  if (openParenIndex === -1 || closeParenIndex === -1 || closeParenIndex <= openParenIndex) {
    return block;
  }

  const inner = block.slice(openParenIndex + 1, closeParenIndex);
  const lines = inner.split('\n');
  const columnEntries = [];
  const constraintLines = [];

  lines.forEach((rawLine) => {
    const trimmed = rawLine.trim();

    if (!trimmed) {
      return;
    }

    if (/^CONSTRAINT\b/i.test(trimmed)) {
      constraintLines.push(trimmed.replace(/,\s*$/, ''));
      return;
    }

    const parsed = splitColumnDefinition(rawLine);
    if (!parsed) {
      return;
    }
    columnEntries.push(parsed);
  });

  const formattedColumns = columnEntries
    .map((entry, index) => {
      const suffix = index === columnEntries.length - 1 ? '' : ',';
      const commentPart = entry.comment ? ` ${entry.comment}` : '';

      if (entry.isRaw) {
        return `    ${entry.raw}${suffix}${commentPart}`;
      }

      const cleanType = entry.type.replace(/"/g, '');
      const clausePart = entry.clause ? ` ${qualifyReferences(entry.clause)}` : '';
      return `    "${entry.name}" ${cleanType}${clausePart}${suffix}${commentPart}`;
    })
    .join('\n');

  const createStatement = `${block.slice(0, openParenIndex).trimEnd()}\n(\n${formattedColumns}\n);`;
  const alterStatements = constraintLines
    .map((constraint) => `ALTER TABLE ${tableName}\n    ADD ${qualifyReferences(quoteConstraintColumns(constraint))};`)
    .join('\n');

  if (alterStatements) {
    return `${createStatement}\n${alterStatements}`;
  }

  return createStatement;
}

function splitColumnDefinition(rawLine) {
  const trimmedLine = rawLine.trim();
  if (!trimmedLine) {
    return null;
  }

  const commentIndex = trimmedLine.indexOf('--');
  let comment = '';
  let definitionPortion = trimmedLine;

  if (commentIndex !== -1) {
    comment = trimmedLine.slice(commentIndex).trimEnd();
    definitionPortion = trimmedLine.slice(0, commentIndex);
  }

  definitionPortion = definitionPortion.replace(/,\s*$/, '').trim();

  if (!definitionPortion) {
    return null;
  }

  const columnMatch = definitionPortion.match(/^"?([A-Za-z0-9_]+)"?\s+(.+)$/i);

  if (!columnMatch) {
    return {
      raw: definitionPortion,
      comment,
      isRaw: true
    };
  }

  const columnName = columnMatch[1];
  const remainder = columnMatch[2].trim();

  if (columnName.toUpperCase() === 'PRIMARY' && /^KEY\b/i.test(remainder)) {
    return {
      raw: formatPrimaryKeyClause(remainder),
      comment,
      isRaw: true
    };
  }

  if (!remainder) {
    return {
      raw: definitionPortion,
      comment,
      isRaw: true
    };
  }

  const keywordPattern = /\b(NOT\s+NULL|NULL|DEFAULT|GENERATED|PRIMARY\s+KEY|REFERENCES|UNIQUE|CHECK|COLLATE|CONSTRAINT)\b/i;
  const keywordMatch = keywordPattern.exec(remainder);

  let typePart;
  let clausePart = '';

  if (keywordMatch) {
    typePart = remainder.slice(0, keywordMatch.index).trim();
    clausePart = remainder.slice(keywordMatch.index).trim();
  } else {
    typePart = remainder.trim();
  }

  if (!typePart) {
    return {
      raw: definitionPortion,
      comment,
      isRaw: true
    };
  }

  let normalizedType = typePart.replace(/"([A-Za-z0-9_]+)"/g, '$1');
  normalizedType = normalizedType.replace(/\s+/g, ' ');
  normalizedType = normalizedType.replace(/[a-z]+/gi, (match) => match.toUpperCase());

  let normalizedClause = clausePart.replace(/"([A-Za-z0-9_]+)"/g, '"$1"');
  const clauseReplacements = [
    { regex: /\bnot\s+null\b/gi, value: 'NOT NULL' },
    { regex: /\bnull\b/gi, value: 'NULL' },
    { regex: /\bdefault\b/gi, value: 'DEFAULT' },
    { regex: /\bgenerated\b/gi, value: 'GENERATED' },
    { regex: /\bby\b/gi, value: 'BY' },
    { regex: /\bprimary\s+key\b/gi, value: 'PRIMARY KEY' },
    { regex: /\breferences\b/gi, value: 'REFERENCES' },
    { regex: /\bunique\b/gi, value: 'UNIQUE' },
    { regex: /\bcheck\b/gi, value: 'CHECK' },
    { regex: /\bconstraint\b/gi, value: 'CONSTRAINT' },
    { regex: /\bidentity\b/gi, value: 'IDENTITY' },
    { regex: /\bdeferrable\b/gi, value: 'DEFERRABLE' },
    { regex: /\binitially\b/gi, value: 'INITIALLY' },
    { regex: /\bdeferred\b/gi, value: 'DEFERRED' },
    { regex: /\bimmediate\b/gi, value: 'IMMEDIATE' },
    { regex: /\bset\b/gi, value: 'SET' },
    { regex: /\bnulls\b/gi, value: 'NULLS' },
    { regex: /\basc\b/gi, value: 'ASC' },
    { regex: /\bdesc\b/gi, value: 'DESC' }
  ];

  clauseReplacements.forEach(({ regex, value }) => {
    normalizedClause = normalizedClause.replace(regex, value);
  });

  normalizedClause = normalizedClause.replace(/\s+/g, ' ').trim();

  return {
    name: columnName,
    type: normalizedType,
    clause: normalizedClause,
    comment: comment || '',
    isRaw: false
  };
}

let tableMetadataCache = null;

function setTableMetadata(metadata) {
  tableMetadataCache = metadata;
}

function quoteConstraintColumns(constraint) {
  if (!constraint) {
    return constraint;
  }

  let result = constraint.replace(/FOREIGN\s+KEY\s*\(([^)]+)\)/gi, (match, columnList) => {
    const columns = columnList
      .split(',')
      .map(col => col.trim())
      .filter(Boolean)
      .map(col => {
        const cleaned = col.replace(/^["']|["']$/g, '');
        return `"${cleaned}"`;
      })
      .join(', ');
    
    return `FOREIGN KEY (${columns})`;
  });

  result = result.replace(/PRIMARY\s+KEY\s*\(([^)]+)\)/gi, (match, columnList) => {
    const columns = columnList
      .split(',')
      .map(col => col.trim())
      .filter(Boolean)
      .map(col => {
        const cleaned = col.replace(/^["']|["']$/g, '');
        return `"${cleaned}"`;
      })
      .join(', ');
    
    return `PRIMARY KEY (${columns})`;
  });

  return result;
}

function qualifyReferences(segment) {
  if (!segment) {
    return segment;
  }

  return segment.replace(/REFERENCES\s+(?:public\.)?["']?([A-Za-z0-9_]+)["']?\s*\(([^)]+)\)/gi, (match, tableName, columnList) => {
    const tableNameLower = tableName.toLowerCase();
    const tableMetadata = tableMetadataCache ? tableMetadataCache.get(tableNameLower) : null;
    
    const columns = columnList
      .split(',')
      .map(col => col.trim())
      .filter(Boolean)
      .map(col => {
        const cleaned = col.replace(/^["']|["']$/g, '');
        
        if (tableMetadata) {
          const columnMeta = tableMetadata.columns.find(c => c.name.toLowerCase() === cleaned.toLowerCase());
          if (columnMeta) {
            return `"${columnMeta.name}"`;
          }
        }
        
        return `"${cleaned}"`;
      })
      .join(', ');
    
    return `REFERENCES public."${tableName}"(${columns})`;
  });
}

function extractTableName(relativePath) {
  const match = relativePath.match(/tables\/(?:[^\/]+\/)*([^\/]+)\.sql$/i);
  return match ? match[1].toLowerCase() : null;
}

function extractTableMetadata(sql) {
  const createTableRegex = /CREATE\s+TABLE\s+IF\s+NOT\s+EXISTS\s+(?:public\.)?["']?([A-Za-z0-9_]+)["']?\s*\(/i;
  const tableMatch = createTableRegex.exec(sql);
  
  if (!tableMatch) {
    return null;
  }
  
  const tableName = tableMatch[1].toLowerCase();
  const columns = [];
  
  const openParenIndex = sql.indexOf('(', tableMatch.index);
  const closeParenIndex = findClosingParenthesis(sql, openParenIndex);
  
  if (openParenIndex === -1 || closeParenIndex === -1) {
    return { tableName, columns };
  }
  
  const inner = sql.slice(openParenIndex + 1, closeParenIndex);
  const lines = inner.split('\n');
  
  lines.forEach(line => {
    const trimmed = line.trim();
    if (!trimmed || /^CONSTRAINT\b/i.test(trimmed) || /^PRIMARY\s+KEY\b/i.test(trimmed)) {
      return;
    }
    
    const parsed = splitColumnDefinition(line);
    if (parsed && !parsed.isRaw) {
      columns.push({ 
        name: parsed.name,
        type: parsed.type,
        clause: parsed.clause
      });
    }
  });
  
  return { tableName, columns };
}

function extractReferencedTables(sql) {
  const referenced = [];
  const regex = /REFERENCES\s+(?:public\.)?["']?([A-Za-z0-9_]+)["']?\s*\(/gi;
  let match;

  while ((match = regex.exec(sql)) !== null) {
    referenced.push(match[1].toLowerCase());
  }

  return referenced;
}

function topologicalSortTables(tables) {
  const tableMap = new Map();
  const dependencies = new Map();

  tables.forEach(entry => {
    const tableName = extractTableName(entry.relative);
    if (tableName) {
      tableMap.set(tableName, entry);
      const refs = extractReferencedTables(entry.sql);
      dependencies.set(tableName, refs);
    }
  });

  const sorted = [];
  const visited = new Set();
  const visiting = new Set();

  function visit(tableName) {
    if (visited.has(tableName)) {
      return;
    }

    if (visiting.has(tableName)) {
      return;
    }

    visiting.add(tableName);

    const deps = dependencies.get(tableName) || [];
    deps.forEach(dep => {
      if (tableMap.has(dep)) {
        visit(dep);
      }
    });

    visiting.delete(tableName);
    visited.add(tableName);

    const entry = tableMap.get(tableName);
    if (entry) {
      sorted.push(entry);
    }
  }

  Array.from(tableMap.keys()).forEach(tableName => {
    visit(tableName);
  });

  return sorted;
}

function convertSelectAssignments(body) {
  const pattern = /SELECT\s+([\s\S]+?)\s+FROM\s+([\s\S]+?);/gi;

  return body.replace(pattern, (match, selectSegment, fromSegment) => {
    const assignments = selectSegment
      .split(',')
      .map((part) => part.trim())
      .filter(Boolean);

    const expressions = [];
    const targets = [];
    let hasAssignment = false;

    assignments.forEach((assignment) => {
      const assignmentMatch = assignment.match(/([^=]+)=\s*(.+)/);

      if (assignmentMatch) {
        hasAssignment = true;
        const [, varPart, exprPart] = assignmentMatch;
        expressions.push(exprPart.trim());
        targets.push(varPart.trim());
      } else {
        expressions.push(assignment);
      }
    });

    if (!hasAssignment || !targets.length) {
      return match;
    }

    const fromClause = fromSegment.replace(/;\s*$/i, '');
    const selectClause = `SELECT ${expressions.join(', ')}`;
    const rebuilt = `${selectClause}\nFROM ${fromClause}\nINTO ${targets.join(', ')};`;

    return rebuilt;
  });
}

function finalizeProcedureBlocks(sql) {
  const pattern = /(CREATE\s+(?:OR\s+REPLACE\s+)?PROCEDURE[\s\S]+?)(?=(?:\n\s*CREATE\s+(?:OR\s+REPLACE\s+)?PROCEDURE)|(?:\n--\s*File:)|$)/gi;

  return sql.replace(pattern, (block) => {
    if (!/CREATE\s+(?:OR\s+REPLACE\s+)?PROCEDURE/i.test(block)) {
      return block;
    }

    let working = block.trimEnd();

    // Remove any existing LANGUAGE clauses so we can reinsert them consistently
    working = working.replace(/\s*LANGUAGE\s+plpgsql\s*/gi, '\n');

    if (!/AS\s*\$\$/i.test(working)) {
      working = working.replace(/AS\b/i, 'AS $$$\n');
    }

    const asMatch = working.match(/AS\s*\$\$/i);
    if (!asMatch) {
      return block;
    }

    const header = working.slice(0, asMatch.index).trimEnd();
    let body = working.slice(asMatch.index + asMatch[0].length).replace(/\$\$;?\s*$/i, '').trim();

    const bodyLines = body.split('\n');
    const declareLines = [];
    const remainingLines = [];

    bodyLines.forEach((line) => {
      const trimmed = line.trim();

      if (!trimmed) {
        return;
      }

      const declareMatch = trimmed.match(/^DECLARE\s+@?([A-Za-z0-9_".]+)\s+([^=;]+?)(?:\s*=\s*(.+))?;?$/i);

      if (declareMatch) {
        const [, varName, typePartRaw, defaultPartRaw] = declareMatch;
        const typePart = typePartRaw.trim();
        let declaration = `${varName.trim()} ${typePart}`;

        if (defaultPartRaw) {
          declaration += ` := ${defaultPartRaw.trim()}`;
        }

        declaration += ';';
        declareLines.push(declaration);
      } else {
        remainingLines.push(line);
      }
    });

    const declareBlock = declareLines.length
      ? `DECLARE\n    ${declareLines.join('\n    ')}\n`
      : '';

    let bodyContent = remainingLines.join('\n').trim();
    bodyContent = convertSelectAssignments(bodyContent);

    if (!bodyContent) {
      bodyContent = 'BEGIN\n    NULL;\nEND;';
    } else if (!/^BEGIN/i.test(bodyContent)) {
      bodyContent = `BEGIN\n${bodyContent}\nEND;`;
    } else if (!/END\s*;?$/i.test(bodyContent)) {
      bodyContent = `${bodyContent}\nEND;`;
    }

    const rebuiltBody = `${declareBlock}${bodyContent}`.trim() + '\n';

    const headerWithLanguage = `${header}\nLANGUAGE plpgsql`;

    return `${headerWithLanguage}\nAS $$\n${rebuiltBody}\n$$;`;
  });
}

function ensureDir(dirPath) {
  if (!fs.existsSync(dirPath)) {
    fs.mkdirSync(dirPath, { recursive: true });
  }
}

function cleanPostgresql(cb) {
  const removeSqlFiles = (dirPath) => {
    if (!fs.existsSync(dirPath)) {
      return;
    }

    const entries = fs.readdirSync(dirPath, { withFileTypes: true });

    entries.forEach((entry) => {
      const fullPath = path.join(dirPath, entry.name);

      if (entry.isDirectory()) {
        removeSqlFiles(fullPath);
        return;
      }

      if (entry.isFile() && entry.name.toLowerCase().endsWith('.sql')) {
        fs.unlinkSync(fullPath);
      }
    });
  };

  ensureDir(TARGET_ROOT);
  removeSqlFiles(TARGET_ROOT);
  cb();
}

function buildSequenceMetadata() {
  if (cachedSequences) {
    return cachedSequences;
  }

  const sequenceDir = path.join(SOURCE_ROOT, SEQUENCE_FOLDER_NAME);
  const sequences = {};

  if (!fs.existsSync(sequenceDir)) {
    cachedSequences = sequences;
    return sequences;
  }

  const files = fs.readdirSync(sequenceDir, { withFileTypes: true });

  files.forEach((entry) => {
    if (!entry.isFile() || !entry.name.toLowerCase().endsWith('.sql')) {
      return;
    }

    const fullPath = path.join(sequenceDir, entry.name);
    const raw = fs.readFileSync(fullPath, 'utf8').replace(/\r\n/g, '\n');
    const nameMatch = raw.match(/CREATE\s+SEQUENCE\s+(?:\[[^\]]+\]\.)?\[?(\w+)\]?/i);

    if (!nameMatch) {
      return;
    }

    const startMatch = raw.match(/START\s+WITH\s+(\d+)/i);
    const incrementMatch = raw.match(/INCREMENT\s+BY\s+(\d+)/i);

    const name = nameMatch[1];

    sequences[name] = {
      name,
      start: startMatch ? Number(startMatch[1]) : 1,
      increment: incrementMatch ? Number(incrementMatch[1]) : 1
    };
  });

  cachedSequences = sequences;
  return sequences;
}

function writePostgresSequences(cb) {
  const sequences = buildSequenceMetadata();
  const destDir = path.join(TARGET_ROOT, SEQUENCE_FOLDER_NAME);
  ensureDir(destDir);

  Object.values(sequences).forEach((seq) => {
    const sequenceSql = [
      `CREATE SEQUENCE IF NOT EXISTS public."${seq.name}"`,
      `    INCREMENT BY ${seq.increment}`,
      `    START WITH ${seq.start}`,
      '    NO MINVALUE',
      '    NO MAXVALUE',
      '    CACHE 1;',
      ''
    ].join('\n');

    const destPath = path.join(destDir, `${seq.name}.sql`);
    fs.writeFileSync(destPath, sequenceSql, 'utf8');
  });

  cb();
}

function getSqlFiles(dir) {
  const results = [];
  const entries = fs.readdirSync(dir, { withFileTypes: true });

  entries.forEach((entry) => {
    const fullPath = path.join(dir, entry.name);

    if (entry.isDirectory()) {
      if (fullPath.toLowerCase() === TARGET_ROOT.toLowerCase()) {
        return;
      }
      results.push(...getSqlFiles(fullPath));
    } else if (entry.isFile() && entry.name.toLowerCase().endsWith('.sql')) {
      results.push(fullPath);
    }
  });

  return results;
}

function replaceSequences(sql) {
  const sequences = buildSequenceMetadata();
  let result = sql;

  Object.keys(sequences).forEach((name) => {
    const pattern = new RegExp(
      `NEXT\\s+VALUE\\s+FOR\\s+(?:public\\.)?(?:"${name}"|\"${name}\"|\\[${name}\\]|${name})`,
      'gi'
    );
    result = result.replace(pattern, `nextval('public."${name}"')`);
  });

  return result;
}

function convertSqlContent(content, relativePath) {
  if (relativePath && overrideGenerators[relativePath]) {
    return overrideGenerators[relativePath]().trim() + '\n';
  }

  let sql = content.replace(/\r\n/g, '\n');

  nonPortableStatements.forEach((pattern) => {
    sql = sql.replace(pattern, '');
  });

  sql = sql.replace(/^\s*GO\s*$/gim, '');

  sql = sql.replace(/PRIMARY KEY CLUSTERED/gi, 'PRIMARY KEY');
  sql = sql.replace(/UNIQUE CLUSTERED/gi, 'UNIQUE');
  sql = sql.replace(/IDENTITY\s*\(\s*\d+\s*,\s*\d+\s*\)/gi, 'GENERATED BY DEFAULT AS IDENTITY');
  sql = sql.replace(/\[dbo\]\./gi, 'public.');
  sql = sql.replace(/dbo\./gi, 'public.');
  sql = sql.replace(/\[(\w+)\]/g, '"$1"');

  sql = sql.replace(/GETUTCDATE\(\)/gi, 'CURRENT_TIMESTAMP');
  sql = sql.replace(/GETDATE\(\)/gi, 'CURRENT_TIMESTAMP');
  sql = sql.replace(/SYSDATETIME\(\)/gi, 'CURRENT_TIMESTAMP');
  sql = sql.replace(/NEWID\(\)/gi, 'uuid_generate_v4()');

  sql = replaceSequences(sql);

  typeReplacements.forEach(({ regex, value }) => {
    sql = sql.replace(regex, value);
  });

  sql = normalizeIndexStatements(sql);
  sql = finalizeIndexBlocks(sql);
  sql = normalizeProcedureParameters(sql);
  sql = finalizeProcedureBlocks(sql);
  sql = ensureCreateGuards(sql);
  sql = normalizeBooleanDefaults(sql);
  sql = extractTableConstraints(sql);
  sql = sanitizeTableConstraints(sql);
  sql = removeTrailingTableCommas(sql);
  sql = stripTsqlVariables(sql);
  sql = ensureStatementTerminators(sql);

  return sql.trim() + '\n';
}

function convertSqlFiles(cb) {
  const files = getSqlFiles(SOURCE_ROOT).filter((filePath) => {
    return !filePath.toLowerCase().includes(`${path.sep}${SEQUENCE_FOLDER_NAME.toLowerCase()}${path.sep}`);
  });

  files.forEach((filePath) => {
    const relative = path.relative(SOURCE_ROOT, filePath);
    const destination = path.join(TARGET_ROOT, relative);
    ensureDir(path.dirname(destination));

    const content = fs.readFileSync(filePath, 'utf8');
    const converted = convertSqlContent(content, relative.replace(/\\/g, '/'));

    fs.writeFileSync(destination, converted, 'utf8');
  });

  cb();
}

function compilePostgresSql(cb) {
  if (!fs.existsSync(TARGET_ROOT)) {
    cb(new Error('postgresql folder not found. Run "gulp convert" first.'));
    return;
  }

  const files = getSqlFiles(TARGET_ROOT)
    .filter((filePath) => filePath.toLowerCase().endsWith('.sql'))
    .filter((filePath) => filePath.toLowerCase() !== COMPILED_OUTPUT.toLowerCase())
    .sort((a, b) => {
      const relA = path.relative(TARGET_ROOT, a).replace(/\\/g, '/');
      const relB = path.relative(TARGET_ROOT, b).replace(/\\/g, '/');

      const priority = (relative) => {
        const lower = relative.toLowerCase();
        if (lower.startsWith('sequences/')) {
          return 0;
        }
        if (lower.startsWith('tables/')) {
          return 1;
        }
        if (lower.startsWith('indexes/')) {
          return 2;
        }
        return 3;
      };

      const priorityDiff = priority(relA) - priority(relB);
      if (priorityDiff !== 0) {
        return priorityDiff;
      }

      return relA.localeCompare(relB);
    });

  if (!files.length) {
    cb(new Error('No PostgreSQL SQL files were found to compile.'));
    return;
  }

  const fileEntries = files.map((filePath) => {
    const relative = path.relative(TARGET_ROOT, filePath).replace(/\\/g, '/');
    const sql = fs.readFileSync(filePath, 'utf8').trim();
    return { relative, sql };
  });

  const composeBundle = (entries) =>
    entries
      .map(({ relative, sql }) => `-- File: ${relative}\n${sql}\n`)
      .join('\n');

  const tablesEntries = [];
  const functionsEntries = [];
  const proceduresEntries = [];

  fileEntries.forEach((entry) => {
    const lower = entry.relative.toLowerCase();

    if (
      lower.startsWith('tables/') ||
      lower.startsWith('indexes/') ||
      lower.startsWith('sequences/')
    ) {
      tablesEntries.push(entry);
    }

    if (lower.startsWith('functions/')) {
      functionsEntries.push(entry);
    }

    if (lower.startsWith('stored procedures/')) {
      proceduresEntries.push(entry);
    }
  });

  const tablesOutputPath = path.join(TARGET_ROOT, 'postgresql-tables.sql');
  const functionsOutputPath = path.join(TARGET_ROOT, 'postgresql-functions.sql');
  const proceduresOutputPath = path.join(TARGET_ROOT, 'postgresql-procedures.sql');

  const extensionGuards = [
    'CREATE EXTENSION IF NOT EXISTS "uuid-ossp";',
    'CREATE EXTENSION IF NOT EXISTS "vector";',
    ''
  ].join('\n');

  const sequences = tablesEntries.filter(e => e.relative.toLowerCase().startsWith('sequences/'));
  const indexes = tablesEntries.filter(e => e.relative.toLowerCase().startsWith('indexes/'));
  const tables = tablesEntries.filter(e => e.relative.toLowerCase().startsWith('tables/'));

  const tableMetadata = new Map();
  tables.forEach(entry => {
    const metadata = extractTableMetadata(entry.sql);
    if (metadata) {
      tableMetadata.set(metadata.tableName, metadata);
    }
  });
  
  setTableMetadata(tableMetadata);

  const sortedTables = topologicalSortTables(tables);

  tablesEntries.length = 0;
  tablesEntries.push(...sequences, ...sortedTables, ...indexes);

  if (tablesEntries.length) {
    const tablesBundle = `${extensionGuards}${composeBundle(tablesEntries)}`;
    fs.writeFileSync(tablesOutputPath, tablesBundle, 'utf8');
  }

  if (functionsEntries.length) {
    fs.writeFileSync(functionsOutputPath, composeBundle(functionsEntries), 'utf8');
  }

  if (proceduresEntries.length) {
    fs.writeFileSync(proceduresOutputPath, composeBundle(proceduresEntries), 'utf8');
  }

  const contents = fileEntries.map(({ relative, sql }) => `-- File: ${relative}\n${sql}\n`);

  ensureDir(TARGET_ROOT);
  fs.writeFileSync(COMPILED_OUTPUT, contents.join('\n'), 'utf8');
  cb();
}

const convert = series(cleanPostgresql, writePostgresSequences, convertSqlFiles);
const compile = series(compilePostgresSql);

exports.convert = convert;
exports.compile = compile;
exports.default = convert;
