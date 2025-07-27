

const toCamelCase = (str) => {
    return str.replace(/([-_][a-z])/ig, ($1) => {
        return $1.toUpperCase()
            .replace('-', '')
            .replace('_', '');
    }).replace(/^./, (match) => match.toLowerCase()); // Ensure first letter is lowercase
}

//forces a json object to have camelCase keys on all properties in the hierarchy
const camelCaseKeys = (obj) => {
    if (Array.isArray(obj)) {
        return obj.map(v => camelCaseKeys(v));
    } else if (typeof obj === 'object' && obj !== null) {
        return Object.keys(obj).reduce((acc, key) => {
            acc[toCamelCase(key)] = camelCaseKeys(obj[key]);
            return acc;
        }, {});
    }
    return obj;
}

export { 
    toCamelCase, 
    camelCaseKeys 
};