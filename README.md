# keychain-sample-dotnet-console

Sample test program using Keychain 

This program illustrates the core functionality of Keychain.

It gives an example of two devices pairing and 
encrypting / signing messages, exchanging and decrypting/verifying 
those message.

NOTES:

Asserts are commented out but left in this code in case you would like to run as a Visual Studio test case

The code is annotated with notes where paths should be modified by the developer.


# Installation

1. Clone this repo.

1. Download the Keychain Core .NET for x86 package. The package version should match the version in the commit tag on this repo.

1. Unzip the package and move the unzipped directory into a location you wish.

1. Set or confirm that the system environment variable PATH includes this location so that Visual Studio will find it upon execution.

1. Copy the keychain.sql and drop_keychain.sql files into a location of your choosing.

1. Do the same for the keychain.cfg file.

1. Change the paths to the .sql and .cfg files in the file Program.cs and check that they are consistent with your chosen file location for each.

1. Build this project and run in Visual Studio.
