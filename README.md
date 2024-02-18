**Deployment and Execution Guide**

Description of Steps for Deploying the System on a Host for Testing
This guide provides instructions for installing, running, and configuring the system for testing purposes. To successfully deploy and run the system, you will need the following components:

•	A computer running Windows, Linux, or macOS.

•	RabbitMQ - a message broker for data exchange between different components of the system.

•	The provided source code of the services: FileParserService and DataProcessorService.

•	Configuration files appsettings.json to configure connection parameters and other settings for the services.

**Instructions for Installing and Running Services**

**1.**	*Installing RabbitMQ:*

•	Download and install RabbitMQ from the official website according to the instructions for your operating system.

**2.**	*Cloning the Repository:*

•	Clone the repository with the source code of the services FileParserService and DataProcessorService to your local computer.

**3.**	*Configuring Connection Parameters:*

•	Edit the appsettings.json file in each of the services (FileParserService and DataProcessorService) to specify the connection parameters to RabbitMQ and other necessary settings.

**4.**	*Building and Running Services:*

•	Open the command prompt or terminal in the root folder of each service.

•	Build the project using the build tool of your development environment (e.g., dotnet build for .NET Core).

•	Start each service using the run command of your development environment (e.g., dotnet run for .NET Core).

**5.**	*Checking System Operation:*

•	Ensure that the services are successfully started and running without errors.

•	Monitor the logs of each service to verify the processing of files and data exchange via RabbitMQ.

**Steps for Creating and Configuring the Database**

**1.**	*Installation and Configuration of SQLite DBMS:*

•	For each service (e.g., DataProcessorService), edit the appsettings.json file and specify the path to the SQLite database file.

•	Ensure that SQLite is installed on your computer.

•	If necessary, create an empty SQLite database file at the specified location.

**2.**	*Database Initialization:*

•	Start the DataProcessorService service and ensure that the database is successfully initialized and ready for operation.

These steps will allow you to successfully deploy and run the system for testing on your host.
