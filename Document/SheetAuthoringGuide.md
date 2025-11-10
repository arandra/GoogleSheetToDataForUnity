# Sheet Authoring Guide

GoogleSheetToData interprets each Google Sheet tab as either **Table** or **Const** mode. Follow the structure below so the Unity generator and provided samples behave consistently.

## Shared Rules
- Row 1 defines **field types** (e.g., `string`, `int`, `float`, `bool`, `List<string>`, `Pair<string, int>`).
- Row 2 defines **field names**. Use PascalCase and avoid spaces/special characters.
- The sheet name becomes the generated class name (`FieldTransform`, `InitConst`, etc.).
- The sheet ID comes from the spreadsheet URL segment `/d/<ID>/`. Sample data references `1_2Y3BtltwsyXTovWuWV6J32x_Ebe2Sy8vybGNhzkIsM`.

## Table Mode
| Row | Description |
| --- | --- |
| 1 | Field types |
| 2 | Field names |
| 3+ | Data rows (each row becomes an entry in the generated list) |

Example:
| string | int | float |
| --- | --- | --- |
| Name | Level | Rate |
| Knight | 10 | 0.12 |
| Archer | 7 | 0.08 |

- Output: base class `FieldTransform` plus ScriptableObject `FieldTransforms` containing `List<FieldTransform> Values`.
- Use `SerializableTypes.Pair<TKey, TValue>` for pair columns. Enter values as `(key, value)`; arrays of pairs use comma-separated tuples.

## Const Mode
- Each row follows `Type / Name / Value`.
- Values serialize verbatim; wrap pair entries in parentheses and separate list items with commas.
- Output: ScriptableObject with a single `Value` field instead of a list.

Example:
| string | GameTitle | GSheet Heroes |
| --- | --- | --- |
| int | DefaultLives | 3 |
| Pair<string,int>[] | StarterItems | `(Sword,1),(Potion,5)` |

## Output Paths & Namespaces
- Script/asset output paths must live under `Assets/`. Relative inputs are recommended.
- Leaving the namespace empty generates global-scope classes.

## Samples
- **FieldTransform**: Table workflow example (`Samples~/FieldTransformSample`).
- **InitConst**: Const workflow example (`Samples~/InitConstSample`).

Import a sample, follow the JSON instructions, and run the generator after finishing OAuth configuration.
