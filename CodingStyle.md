# C# Coding Style

1. Braces begin on a new line. Single line statement blocks without braces are allowed,
but should only be used when it can all fit on the same line. An exception to this is
when the following statement has a statement following itself.

**Examples:**  
```csharp
if (count > 5) count = 0;
```
```csharp
if (baseType.IsFloat())
    while (value > 5)
    {
        ...
    }
```
2. Four spaces are used for indentation, tabs should not be used for indentation.
3. `_camelCase` should be used for private properties and fields. For non-private properties/fields, functions,
classes and namespaces, `PascalCase` is used. Local variables are always in `camelCase`.
4. Always specify visibility and whether or not a property should be "settable" at all.
5. `this.` should be avoided unless absolutely necessary.
6. `var` is only used when the type is obvious, and not a *basic* type that is built-in C# (eg. string, int, decimal).
7. More than one empty line at a time should always be avoided. Blocks with braces should be followed by a new line,
unless it's a part of a variable declaration. `return` statements should always be separated from other statements with
a new line. The child statements of "one"-liner if/else statements should be aligned, eg.
```csharp
if (partValue != currentValue) currentValue++;
else                           currentValue = 0;
```
8. Comments start with a space and an upper case letter, and end with a period. Comments may be split up over several lines.

**Examples:**  
```csharp
// Lorem ipsum dolor sit amet, consectetur adipiscing elit,
// sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.```

9. Properties are sorted it groups of visibility, with a new line separating each group.
Groups are sorted by their visibility level, eg. public goes first.
Every part is aligned with the other properties in the group, like so:
```csharp
public string DisplayName { get; }
public int    Points      { get; set; }

private int        _loginAttempts;
private DateTime[] _dates;
```

10. Top-level public "variables" in classes should be properties and should have appropriate
getters/setters.
