# CodeMaid+

This simple extension (see [GitHub home page](https://github.com/oleg-shilo/CodeMaidPlus)) is an attempt to address some shortcomings of rather excellent code formatting extension [CodeMaid](http://www.codemaid.net/). The best outcome is achieved when it is used (integrated) with CodeMaid even though it can be used on its own.

## Problem

One of the most popular and versatile coding assistance solution, ReSharper, has a fundamental flaw. Its outdated formatting strategy is extremely aggressive and very often stays on the way of being productive. Thus it does not support some modern syntax paradigms nor allow applying styles in a less aggressive way. Basically when it comes to formatting this great product becomes rather stubborn.

The problem has been consistently reported to (and acknowledged by) JetBrains since as early as 10-15 years ago. Though their support always reject any need for improvement. If you google you will be able to find the threads where ReSHarper support simply advise the users to stop using ReSharper formatting if they don't like it as is.  

For people who don't want to give up on ReSharper and yet demand more than its can do for formatting the solution can be to use it in conjunction with CodeMaid. CodeMaid is specifically developed as a versatile formatting solution thus it can be used in conjunction with ReSharper. CodeMaid for formatting and ReSharper for everything else.  

This approach usually works very well. However due to the more liberal nature of the formatting algorithm (something that makes CodMaid so great) it can leave some formatting artefacts that need to be addressed manually.

This extension is an attempt to address these CodeMaid shortcomings by extending CodeMaid functionality with additional formatting algorithms that are automatically invoked during CodeMaid cleaning/formatting execution. 
You just need to configure CodeMaid to invoke _CodeMaid+_ during cleanup (see _**Installation**_ section)

## Solution

The following formatting actions are performed during _CodeMaid+_ execution:

### Sorting using statements 
CodeMaid invokes Visual Studio own 'remove and sort using statements'. If it is done on saving the document it can lead to the accidental loss of some of them. The problem is that VS does both removal and sorting. CodeMaid.Plus fixes this problem by not removing unused 'usings' and still sorting and removing duplicates.

All 'usings' are grouped and all four groups are ordered as below. And all items within a given group are sorted alphabetically:
```
    using System.*
    using Microsoft.*
    using <aliases>
    using <statics>
```

![](https://raw.githubusercontent.com/oleg-shilo/CodeMaidPlus/master/images/using.before.png)
![](https://raw.githubusercontent.com/oleg-shilo/CodeMaidPlus/master/images/using.after.png)

### Fluent is not aligned
CodeMaid/VS more relaxed indentation for fluent API is great but it may get accidentally misaligned and not even gain the first level of indent. CodeMaid+ fixes it by ensuring at least one level of indentation:

![](https://raw.githubusercontent.com/oleg-shilo/CodeMaidPlus/master/images/indent-1.before.png)
![](https://raw.githubusercontent.com/oleg-shilo/CodeMaidPlus/master/images/indent-1.after.png)

In case of more canonical Fluent pattern the original indentation stays untouched.
![](https://raw.githubusercontent.com/oleg-shilo/CodeMaidPlus/master/images/indent-2.after.png)

_**Limitations**_
Currently each line being aligned is handled independently by aligning its indent to the nearest anchor point.

The alignment anchor points for a line are:
- total indent of the previous line
- total indent of the previous line + extra single indent
- start of the special tokens in the previous line:
  - '.' character
  - '(' character
  - "=>" 
  - "=" 
  - "return"
  - ":"

In the future releases the alignment adjustments applied to the text above the line being aligned will be incorporated. The will lead to more harmonious alignment outcome.


### XML Documentation block may contain trailing blank line

A simple problem that is just overlooked by CodeMaid.

![](https://raw.githubusercontent.com/oleg-shilo/CodeMaidPlus/master/images/doc.before.png)
![](https://raw.githubusercontent.com/oleg-shilo/CodeMaidPlus/master/images/doc.after.png)


## Installation

Install the extension from Visual Studio Marketplace and configure it to be invoked during CodeMaid cleanup by placing the command `Tools.CM+Format` in the _CodeMaid > Options > Reorganizing > ThirdParty > Other Cleaning Commands_:

![](https://raw.githubusercontent.com/oleg-shilo/CodeMaidPlus/master/images/config.png)

Alternatively you can open extension setting dialog and press 'Integrate' button. The dialog can be accessed via _View > Other Windows > CM+ Settings_:

![](https://raw.githubusercontent.com/oleg-shilo/CodeMaidPlus/master/images/settings.integrate.png)

## Limitations

- Currently the extension processes only the files that are part of a solution.
- The indent size is assumed to be 4 spaces. It will be read from the VS settings in the future.
- The formatting is based on the canonical C# bracket style. If you are using "Egyptian brackets" it will interfere with the extension _Align Indents_ feature.  

## Conclusion

This solution is an open end effort and it can grow in additional functionality if the need arises. On the other hand, it may be short lived if CodeMaid address its problems natively.
