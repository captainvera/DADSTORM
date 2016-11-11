README - GRUPO 14 

> How to start the project
  - Open the project solution (DADSTORM.sln)
  - Compile all the projects in the solution
  - Copy any input files to the working directory of the replica's (where the Node.exe is located). 
        Example: copy tweeters.dat to %Project Root%/Node/bin/Debug/ if the project is compiled in Debug mode
  - Execute Node.exe (Starts a node that provides de PCS service)
  - Execute Puppetmaster.exe (i.e. %Project Root%/Puppetmaster/bin/Debug/)

 > Unimplemented features
  - The commands from the config file aren't executed automatically when the config file is loaded.
  - Different routing strategies aren't thoroughly tested

