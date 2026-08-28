# RegexLang

## Introduction

`RegexLang` is a silly programming language where almost every operation is based on a *Regular Expression*.

While regex is typically used for matching strings, they can also be used as logic components.
And this language gives regex logics like conditions, loops, methods and exceptions.
It is basically a more advanced, structured and regex-focused version of the sed command.

## RegexLang Syntax

### Data

Data is stored in a dictionary with the key and value only strings.
Functions is stored in another separate dictionary with the value functions.

### Exception

Exception type is stored in `#exc`, description is stored in `#exc_desc` and trace is stored in `#exc_trace`.
+ System exceptions like `parse`(illegal regex) will be thrown like this.
+ Custom exceptions can be thrown by writing `#exc` field, which should be written AFTER writing `#exc_desc`, and `#exc_trace` field will be automatically set.

### Exit Value

To set an exit value, set `#exit` to the value in BINARY.

### Active Variable

Every task keeps track of an active variable, which is specified with a name.
Initially, the name is `var` and the variable is set to a string with characters with the codes from `0` to `255`.

### Variable Referencing

This Works in the `VARIABLE`, `REPLACEMENT_VARIABLE` and `FN_NAME` fields.

+ To get/set the value of a variable, just put the variable name in the field.
+ To get the value of a variable, then get/set the value of a variable with the name the value of the original variable, add an `@` at the beginning of the field.

+ If there is multiple leading `@` symbols, one will be removed, and the rest is directly used as the name of the variable to get/set.
+ If the value is not set, it will be considered `null`(not literal `"null"`).

### Parameter `PATTEN`
An *Extended* Regular Expression, support backtracking

### Parameter `OPTIONS`
The `OPTIONS` can contain `g`(Global), `i`(IgnoreCase), `m`(MultiLine), `s`(SingleLine), case sensitive.

### `s` Command
```rexl
s/<PATTERN>/<REPLACEMENT>/<OPTIONS>
```
If a `/` is used in either the `PATTERN` or the `REPLACEMENT`, it should be escaped as `\/`.

+ It reads, operates then writes the active value.

If the active variable is null, it will throw a `null_active` exception.

### `v` Command
```rexl
v/<PATTERN>/<REPLACEMENT_VARIABLE>/<OPTIONS>
```
If a `/` is used in either the `PATTERN` or the `REPLACEMENT_VARIABLE`, it should be escaped as `\/`.

+ It reads, operates with the value of the variable then writes the active value.

If the active variable is null, it will throw a `null_active` exception.

If the `REPLACEMENT_VARIABLE` is null, then the active variable will be guaranteed to be set to null.

### `r` Command
```rexl
r/<VARIABLE>/<PATTERN>/<REPLACEMENT>/<OPTIONS>
```
If a `/` is used in any of `VARIABLE` the `PATTERN` or the `REPLACEMENT`, it should be escaped as `\/`.

+ It reads and operates the `VARIABLE`, then writes the active value.

If the `VARIABLE` is null, then the active variable will be guaranteed to be set to null.

### `w` Command
```rexl
w/<VARIABLE>/<PATTERN>/<REPLACEMENT>/<OPTIONS>
```
If a `/` is used in any of `VARIABLE` the `PATTERN` or the `REPLACEMENT`, it should be escaped as `\/`.

+ It reads and operates the active value, then writes the `VARIABLE`.

If the active variable is null, it will throw a `null_active` exception.

### `l` Command
```rexl
l/<PATTERN>/<OPTIONS>/
  COMMAND ...
/
```
If a `/` is used in the `PATTERN`, it should be escaped as `\/`.

+ Loop while the active variable does match the pattern.

If the active variable is null, it will be guaranteed to exit.

### `\` Command
```rexl
\
  COMMAND ...
/
```
Define a linear operation list, for code readability.


### `i` Command
```rexl
i/<PATTERN>/<REPLACEMENT>/<OPTIONS>
```
If a `/` is used in either the `PATTERN` or the `REPLACEMENT`, it should be escaped as `\/`.

+ It reads and operates \[characters with the same count as the count of the current active variable\] from the input, and write it to the active variable.

If the active variable is null, it will throw a `null_active` exception.

If the IO failed, it will throw an `io` exception.

### `o` Command
```rexl
o/<PATTERN>/<REPLACEMENT>/<OPTIONS>
```
If a `/` is used in either the `PATTERN` or the `REPLACEMENT`, it should be escaped as `\/`.

+ It reads, operates the active value then prints it, the value is not changed.

If the active variable is null, it will throw a `null_active` exception.

If the IO failed, it will throw an `io` exception.


### `f` Command
```rexl
f/s/<FN_NAME>/
  COMMAND ...
/
```
If a `/` is used in the `FN_NAME`, it should be escaped as `\/`.
+ Define a function.

```rexl
f/u/<FN_NAME>/
```
If a `/` is used in the `FN_NAME`, it should be escaped as `\/`.
+ Remove a function.

```rexl
f/c/<PATTERN>/<REPLACEMENT>/<OPTIONS>
```
If a `/` is used in either the `PATTERN` or the `REPLACEMENT`, it should be escaped as `\/`.

+ It reads, operates the active value then call the function with the operated value as its name.

## Usage

```sh
RegexLang -- CODE.rexl # run rexl code directly as a script
RegexLang run CODE.rexl # run rexl code directly as a script
RegexLang check CODE.rexl # check rexl code
RegexLang shell # enter the interactive shell
```