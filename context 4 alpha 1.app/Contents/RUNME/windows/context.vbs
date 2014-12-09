' Start the Context console, a minimal web server one may use
' from a web browser to control Contexts.

set shell = createobject("wscript.shell")
shell.CurrentDirectory = "..\..\.."
shell.run "..\win32\spoon.exe 946BE974-48B7-4D11-B209-6355B3E49722.image"
' Allow some time for the web server to start.
WScript.Sleep 2000
shell.run "iexplore http://localhost:8090/README.html"
