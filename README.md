# MD2HTML

MD2HTML is my own Markdown to HTML converser. Its main and only use is to convert [notes from my university lectures](https://github.com/ldellisola/ITBA) into HTML for easy access. 

All my notes are written with Typora, but I couldn't find any program that would correctly transform Typora's markdown (mostly latex) into HTML and I got tired of converting every file by hand. Most of the heavy work is taken care of by Pandoc, but I'm manually transforming parts of the Latex code into a format Pandoc can interpret.

Within this project you will find the library I created to do the heavy work, a Console application to run once and a background service. 

The Console application need the directory where your markdown files are and the output where you want all your new files. It will also clone your directory into the destination folder, but you can also filter unwanted files and folders.

The Background services takes this a step further. It will run periodically checking if there is an update in a github repository. If new files have been added, it will convert all the files and publish them to folder. It's essentially some sort of CI/CD set up for my notes. 



For these projects to work you need to install git and Pandoc in your machine



