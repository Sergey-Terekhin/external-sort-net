Implementation of the external merge sort algorithm and test file generator.

Input test file has following format:
```
<Number>. <String>\r\n
```
For Example:
```
415. Apple
30432. Something something something
1. Apple
32. Cherry is the best
2. Banana is yellow
```

Both parts (number and string) may be repeated several times inside the file. It's required 
to generate another file where all lines are sorted by following criteria: string part should 
be compared first. If string parts are equal, numbers should be compared. For example:
```
1. Apple
415. Apple
2. Banana is yellow
32. Cherry is the best
30432. Something something something
```

Two utils should be created:
1. Util to create test file of the required size. 
2. Util to sort test file. Important notice: input file may exceed RAM size, for testing 100Gb file will be used


