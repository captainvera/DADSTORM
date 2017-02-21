README - GRUPO 14 - DADStorm

> Notes
  - in hashing(field_number), field_number is considered the absolute position of the field as ranging from 0 to (length-1) of the tuple

> How to start the project
  - Open the project solution (DADSTORM.sln)
  - Compile all the projects in the solution
  - Copy any input files to the working directory of the replica's (where the Node.exe is located). 
        Example: copy tweeters.dat to %Project Root%/Node/bin/Debug/ if the project is compiled in Debug mode
  - Execute Node.exe (Starts a node that provides de PCS service)
  - Execute Puppetmaster.exe (i.e. %Project Root%/Puppetmaster/bin/Debug/)
  - When prompted, input the location where the replicas can find the Puppetmaster (i.e. localhost)
  - The project should now start running, after the config is parsed
       (for further testing the command readfile <OP> <replica_number> will read the tweeters.dat file from the project root if present and process it's contents)

