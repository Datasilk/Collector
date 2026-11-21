# Module List

[Home](../README.md) > [Journal](../Journal.md) > Module List Module

`type: "module-list"` — a flat nested container: a column of child modules
inside one module.

## Data

```json
{ "type": "module-list", "modules": [ /* nested modules */ ] }
```

## Behavior

- Groups related modules (title + body + gallery) as one reorderable unit.
- Children use the same module shapes as top-level; nesting recursion is
  handled by `module-list.jsx`.
