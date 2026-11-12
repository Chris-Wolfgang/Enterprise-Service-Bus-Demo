type: fix

`{####}` and `{##}` placeholders now cover their full ranges (1000-9999 and 10-99; the upper value was never produced before), and `PlaceholderResolver` only draws a random number or allocates a replacement string for a placeholder the template actually contains.
