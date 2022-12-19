На входе есть большой текстовый файл, где каждая строка имеет вид Number. String
Например:
415. Apple
30432. Something something something
1. Apple
32. Cherry is the best
2. Banana is yellow
Обе части могут в пределах файла повторяться. Необходимо получить на выходе другой файл, где 
все строки отсортированы. Критерий сортировки: сначала сравнивается часть String, если она 
совпадает, тогда Number.
Т.е. в примере выше должно получиться
1. Apple
415. Apple
2. Banana is yellow
32. Cherry is the best
30432. Something something something
Требуется написать две программы:
1. Утилита для создания тестового файла заданного размера. Результатом работы должен быть 
текстовый файл описанного выше вида. Должно быть какое-то количество строк с одинаковой 
частью String.
2. Собственно сортировщик. Важный момент, файл может быть очень большой. Для тестирования 
будет использоваться размер ~100Gb.
При оценке выполненного задания мы будем в первую очередь смотреть на результат 
(корректность генерации/сортировки и время работы), во вторую на то, как кандидат пишет код. 
Язык программирования: C#



1. Есть ли ограничения на используемые пакеты, версии языка и фреймворка?
До .net 6 можно. Ограничений на инструменты особых нет - если они были официально выпущены.
2. Какие ограничения на число?
Желательно, чтобы решение могло обрабатывать числа во всём диапазоне выбранного целочисленного типа.


Может ли число быть очень длинным (не влезать в стандартные int/long)?
Нет, оно влезает в long.


Какие в этом случае к нему предъявляются требования сортировки (сортировать как строку или как число)?
Идея состоит в том что сначала сортируется по тексту, а если текст одинаковый то сортируется по числу.

3. Какие ограничения на строку?
Предположим, 1024 символа в одной строке максимум.

Есть ли заданный символ перевода строки или используется системный?
Каждая строка должна быть отделена от предыдущей carriage return + new line character (\r\n).


Могут ли в строке встречаться цифры или иные не буквы? Если только буквы, то каким алфавитом ограничены слова?
Это может быть любой набор символов из набора a-zA-Z0-9.
4. При сортировке строк должны учитываться эквивалентные символы в некоторых языках? Например, 'ß' и "ss" в немецком. Нет.
5. Должны ли при генерации формироваться осмысленные слова и псевдопредложения?
Подойдет любой способ. При проверке задания основной фокус будет на второй части - сортировщике.





Результаты для генератора тестовых файлов
[21:12:57 INF] Creating 1Gb file for string length 20 bytes
[21:12:57 INF] Started to generate test data file C:\Users\terekhin\Downloads\output_20.txt
[21:12:57 INF] Generated 10000 records to use as duplicates source
[21:18:49 INF] Finished generation of test data file. Generated 43410026 records and 1073741872 bytes. Duplicates written: 438276
[21:18:49 INF] Creating 1Gb file for string length 100 bytes
[21:18:49 INF] Started to generate test data file C:\Users\terekhin\Downloads\output_100.txt
[21:18:49 INF] Generated 10000 records to use as duplicates source
[21:20:42 INF] Finished generation of test data file. Generated 16587496 records and 1073744065 bytes. Duplicates written: 168093
[21:20:42 INF] Creating 1Gb file for string length 500 bytes
[21:20:42 INF] Started to generate test data file C:\Users\terekhin\Downloads\output_500.txt
[21:20:42 INF] Generated 10000 records to use as duplicates source
[21:21:18 INF] Finished generation of test data file. Generated 4054993 records and 1073759336 bytes. Duplicates written: 40888
[21:21:18 INF] Creating 1Gb file for string length 1000 bytes
[21:21:18 INF] Started to generate test data file C:\Users\terekhin\Downloads\output_1000.txt
[21:21:19 INF] Generated 10000 records to use as duplicates source
[21:22:01 INF] Finished generation of test data file. Generated 2086021 records and 1073742026 bytes. Duplicates written: 20823

Вывод - меньше длина строки, менее эффективная генерация



|                               Method | StringLength | ArrayLength |      Mean |    Error |   StdDev |     Gen0 |     Gen1 | Allocated |
|------------------------------------- |------------- |------------ |----------:|---------:|---------:|---------:|---------:|----------:|
|    StringData_StringComparer_Ordinal |          100 |        1000 |  45.19 us | 0.728 us | 0.779 us |        - |        - |      64 B |
|           ByteData_AsciiDataComparer |          100 |        1000 | 139.51 us | 1.095 us | 0.971 us |        - |        - |      88 B |
| ByteData_ConvertToString_And_Compare |          100 |        1000 | 183.22 us | 1.879 us | 1.758 us |  36.8652 |  11.7188 |  232088 B |
|    StringData_StringComparer_Ordinal |         1000 |        1000 |  46.31 us | 0.441 us | 0.391 us |        - |        - |      64 B |
|           ByteData_AsciiDataComparer |         1000 |        1000 | 135.89 us | 1.456 us | 1.362 us |        - |        - |      88 B |
| ByteData_ConvertToString_And_Compare |         1000 |        1000 | 539.15 us | 6.473 us | 5.738 us | 323.2422 | 141.6016 | 2032088 B |


