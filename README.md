# csharp-webcrawler
A webcrawler made in c#

A simple webcrawler in csharp, with all of the relevant visual studio files.
It is run from the command line and takes three arguments: The seed URL, the max recursion depth, and the max number of links crawled per page.

Currently relationships between urls are captured in an HTML DOM. The DOM is also used to handle the data as the crawler generates it. This is problematice because the DOM can only be effectively dealt with on a single thread. Therefore, a significant improvement would be to allow the asynchronous crawler to queue up synchronous calls to the synchronous DOM manager. This way the activity of the crawler is not throttled by slow DOM management.
