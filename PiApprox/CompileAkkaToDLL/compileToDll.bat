@echo off
@echo "Compiling akka to output akka-actors-version.dll"
c:\dev\ikvm\bin\ikvmc.exe -out:akka-actors-2.1.4.dll -target:library -recurse:c:\dev\akka-2.1.4\lib\scala-library.jar c:\dev\akka-2.1.4\lib\akka\config-1.0.0.jar c:\dev\akka-2.1.4\lib\akka\akka-actor_2.10-2.1.4.jar
@echo on