## Model of sub-language to filter data

The essential parts of language are following:

- **Keywords:** and, or, not
- **Brackets:** (, )
- **Comparison operators:** <, <=, >, >=, =, <>
- **Parameteres for filtering by date:** date, time
- **Parameters for filtering by duration:** val
- **Special word:** 'now'

#### Common description

The values of parameters `date`, `time` and `val` must be given in single quotes. To use value of actual date or time one can
use the special word `'now'` (also in single quotes). Using of `now` by filtering by duration is not possible and produces error by executing of request.   
To filter by specific date the parameter `date` must be given in format `dd.MM.yyyy`, i.e. `date = '01.05.2025'`.  
To filter by specific time the parameter `time` must be given in format `hh:mm:ss` or `hh:mm` (in this case the time will be treated as `hh:mm:00`).  
To filter by specific duration the parameter `val` must be given in format `hh:mm:ss` or `hh:mm` (in this case the duration will be treated as `hh:mm:00`).
As an alternative on can also use the format `<hours>H <minutes>M <seconds>S`. The letters `H`, `M` and `S` can be given in lower or in upper case.
Some parts of time spezifications can be absent; in this case the missed part will be replaced with value '0', i.e. `1h 20S` means `01:00:20`.

#### Examples

- **Filter by dates:**  
date > '20.10.2026' and (date <= 'now' or (date > 'now' and time < '23:14')) and not (time = 'now' or time <> '12:27')
- **Filter by duration:**  
(val > '01:20' and val < '2H 10m 30S') or val > '3h'

#### Priority of operators from highest to lowest:
- not
- (, )
- and
- or
- <, <=, >, >=, <>

#### Formal specification of language's structur:
- expression → equality (("and" | "or") expression)*
- equality → comparison (("=" | "<>") comparison)*
- comparison → unary (("<" | "<=" | ">" | ">=") unary)*
- unary → "not" | unary | primary
- primary → STRING | "now" | "(" expression ")"


Idee to implement a scanner / parser for sub-language was taken from here: **[craftinginterpreters.com](https://www.craftinginterpreters.com/contents.html)**.  
The book is short, free and written easy for understanding. Highly recommended :)
