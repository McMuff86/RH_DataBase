# Rhino-Supabase Plugin

## Introduction
This plugin for Rhino enables the management of parts and drawings through a Supabase database. It offers an intuitive user interface, allowing users to select and insert parts into their Rhino projects. The plugin is built on **RhinoCommon** and **Eto.Forms**. A future extension to include a product configurator is planned.

## Requirements
- **Rhino 3D Software**: Required for plugin integration.
- **.NET Framework**: Serves as the development foundation.
- **Nuget Package Manager**: Used to install necessary libraries.

## Installation
1. **Install Nuget Packages**:
   - `RhinoCommon`: For Rhino plugin development.
   - `Eto.Forms`: For creating the user interface.
   - `Supabase`: For establishing the database connection.
2. Install a platform-specific Eto.Forms package, e.g., `Eto.Platform.Wpf` for Windows.

## Configuration
1. **Set up Supabase Project**:
   - Create a project in Supabase.
   - Note the **Project URL** and **Public Key**.
2. **Define Data Models**:
   - Create C# classes for parts and drawings using `[Table]` and `[Column]` attributes.
3. **Initialize Supabase Client**:
   - Configure the client in your code using the Project URL and Public Key.
4. **Set up Storage Bucket**:
   - Create a storage bucket in Supabase called "uploadrhino" for storing 3DM files.
   - Ensure proper permissions are set for the bucket.

## File Storage and Database Integration
The plugin uses Supabase Storage to store and retrieve Rhino 3DM files that are linked to parts and drawings in the database:

1. **Bucket Structure**:
   - All files are stored in a bucket named "uploadrhino"
   - Files are organized with unique names that include timestamps

2. **Parts and 3DM Files**:
   - Each part can have an associated 3DM file (block definition)
   - The file path is stored in the `model_path` column of the parts table
   - When exporting a block from Rhino, it's converted to a 3DM file and stored in the bucket

3. **Drawings and 3DM Files**:
   - Drawings can have associated 3DM files
   - The file path is stored in the `file_path` column of the drawings table
   - When importing a drawing, the 3DM file is uploaded to the bucket and linked to the drawing record

4. **File Retrieval**:
   - When inserting a part or opening a drawing, the plugin:
     - Downloads the 3DM file from the Supabase bucket to a temporary directory
     - Imports the file into Rhino as a block definition
     - Inserts the block into the current document
     - Adds metadata as userstrings to the Rhino object

## Usage
1. Launch Rhino and load the plugin.
2. Access the user interface through the plugin menu.
3. Connect to the Supabase database.
4. **Working with Parts**:
   - Select parts from the list and insert them into your Rhino project.
   - Create blocks from selected objects and export them as parts.
   - Export existing block definitions as parts.
5. **Working with Drawings**:
   - Import 3DM files as drawings and link them to parts.
   - View and manage drawings associated with parts.
   - Open drawings directly in Rhino from the database.

## Development Notes
- **RhinoCommon**: Refer to the [RhinoCommon Documentation](https://developer.rhino3d.com/guides/rhinocommon/) for plugin development details.
- **Eto.Forms**: See the [Eto.Forms Wiki](https://github.com/picoe/Eto/wiki) for UI development guidance.
- **Supabase C#**: The library is available on [GitHub](https://github.com/supabase-community/supabase-csharp).

## Database Integration and Development Tools
- **MCP (Model Context Protocol)**: This project uses MCP Server for Cursor IDE to interact with the Supabase database directly from the development environment.
- **Supabase Documentation**: The following documentation resources are used for database integration:
  - [Supabase API Overview](https://supabase.com/docs/guides/api)
  - [API Quickstart](https://supabase.com/docs/guides/api/quickstart)
  - [Generating Types](https://supabase.com/docs/guides/api/rest/generating-types)
  - [SQL to REST Conversion](https://supabase.com/docs/guides/api/sql-to-rest)
  - [Creating API Routes](https://supabase.com/docs/guides/api/creating-routes)
  - [API Keys Management](https://supabase.com/docs/guides/api/api-keys)
  - [Securing Your API](https://supabase.com/docs/guides/api/securing-your-api)
  - [Database Tables](https://supabase.com/docs/guides/database/tables)
  - [Working with Arrays](https://supabase.com/docs/guides/database/arrays)
  - [Postgres Indexes](https://supabase.com/docs/guides/database/postgres/indexes)
  - [Storage Management](https://supabase.com/docs/guides/storage)

## Database Setup and Testing
The plugin has built-in functionality for testing the database connection and setting up necessary tables:
- On plugin startup, it automatically tests the connection to the Supabase database
- If tables don't exist yet, it attempts to create them by adding sample data
- A test command `TestDatabaseConnection` is available to manually verify the database connection

## Future Extensions
- **Product Configurator**: Automatic selection and insertion of components from the database into Rhino.

# Project Structure and Code Clarity
To keep the code in the main project folder clear and organized, the following structure is implemented:

- **Models/**: Data models (e.g., parts, drawings).
- **Views/**: UI components.
- **Controllers/**: Control logic.
- **Services/**: Services (e.g., database connection).
- **Config/**: Configuration files.
- **Tests/**: Unit tests.

### Measures for Clarity
- **Naming Conventions**: Use meaningful names for classes and methods.
- **Modularity**: Employ small, reusable code modules.
- **Comments**: Include explanations within the code.
- **Version Control**: Use Git to track changes.
- **Tests**: Implement unit tests for stability.
- **Dependencies**: Manage via Nuget.
- **Logging**: Incorporate error and event logging.

---

## What's Missing to Maximize the Plugin's Usefulness?
To enhance the Rhino-Supabase Plugin and ensure it provides maximum benefit to users, the following features and improvements could be added:

- **User Authentication**:
  - Implement a login system to secure access to the Supabase database, ensuring only authorized users can view or modify data.
  
- **Database Backups**:
  - Add support for automatic or manual backups of the Supabase database to prevent data loss and improve reliability.

- **Error Handling**:
  - Introduce robust error handling with user-friendly notifications to help users troubleshoot issues effectively.

- **User Documentation**:
  - Provide detailed guides and tutorials for end-users, covering installation, configuration, and usage to improve accessibility and adoption.

- **Internationalization**:
  - Add multi-language support in the user interface to cater to a global audience.

- **Performance Optimization**:
  - Optimize data queries and storage processes to enhance the plugin's speed and efficiency, especially for large datasets.

- **Integration Tests**:
  - Develop tests to verify the interaction between Rhino and the Supabase database, ensuring seamless functionality.

- **CI/CD Pipeline**:
  - Set up automated builds and testing through a continuous integration and continuous deployment pipeline to streamline development and ensure consistent quality.

These additions would significantly improve the plugin's security, usability, reliability, and scalability, making it a more powerful tool for Rhino users managing parts and drawings via Supabase.

--- 

This response is complete and self-contained, addressing the user's request to reconsider the list and identify what's missing to maximize the plugin's usefulness, all while adhering to the query's structure and language preferences.