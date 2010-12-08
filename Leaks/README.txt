This software is provided AS IS.  No warranty is made to the accuracy of the information presented.
Modify, extend, improve as you will.

Usage

Run your mono application:
MallocStackLogging=1 mono -v <your app> <app arguments> > mono-v.output

While the application is running:
leaks -exclude mono_method_full_name <pid of mono> > leaks.output

Open these two files in the Leaks UI utility. 
