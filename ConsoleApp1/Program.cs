﻿/*
 * 
 * 
 * Sample test program using Keychain 
 * 
 * This program illustrates the core functionality of Keychain.
 * 
 * It gives an example of two devices pairing and 
 * encrypting / signing messages, exchanging and decrypting/verifying 
 * those message.
 * 
 * NOTES:
 * 
 * Asserts are commented out but left in this code in case you would like to run as a Visual Studio test case
 * 
 * The code is annotated with notes where paths should be modified by the developer.
 * 
 * 
 * 
 */



using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using NLog.Config;
using NLog.Targets;

using Keychain;

namespace ConsoleApp1
{
    class Program
    {
        // NOTE: SET THESE PATHS TO YOUR OWN PATHS
        private const string ConfigFile = "C:\\Users\\sundance\\Downloads\\Keychain-DotNet-x86-2.3.2\\bin\\keychain.cfg";
        private const string DropSqlFile = "C:\\Users\\sundance\\Downloads\\Keychain-DotNet-x86-2.3.2\\bin\\drop_keychain.sql";
        private const string CreateSqlFile = "C:\\Users\\sundance\\Downloads\\Keychain-DotNet-x86-2.3.2\\bin\\keychain.sql";
        public static readonly NLog.Logger logger = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            // Configure your logger
            // This is specific to this test program. Your application
            // does not have to use this logging method

            var nconfig = new LoggingConfiguration();

            // NOTE: SET THIS PATH TO YOUR OWN PATHS
            var consoleTarget = new FileTarget() { FileName = "C:\\Users\\sundance\\unittest.log" };
            nconfig.AddTarget("console", consoleTarget);
            nconfig.LoggingRules.Add(new LoggingRule("*", LogLevel.Trace, consoleTarget));

            LogManager.Configuration = nconfig;



            // Create two gateways and two monitors simulating two
            // devices. Don't forget to start the monitor threads!

            // NOTE: SET THIS PATH TO YOUR OWN PATHS
            var gatewayA = new Keychain.Gateway("C:\\Users\\sundance\\keychain-data\\keychain-dotnet-a.db",
                ConfigFile, DropSqlFile, CreateSqlFile, false);

            //Assert.AreNotEqual(null, gatewayA);

            // NOTE: SET THIS PATH TO YOUR OWN PATHS
            var monitorA = new Keychain.Monitor("C:\\Users\\sundance\\keychain-data\\keychain-dotnet-a.db",
                ConfigFile, DropSqlFile, CreateSqlFile);

            //Assert.AreNotEqual(null, monitorA);

            // NOTE: SET THIS PATH TO YOUR OWN PATHS
            var gatewayB = new Keychain.Gateway("C:\\Users\\sundance\\keychain-data\\keychain-dotnet-b.db",
                ConfigFile, DropSqlFile, CreateSqlFile, false);

            //Assert.AreNotEqual(null, gatewayB);

            // NOTE: SET THIS PATH TO YOUR OWN PATHS
            var monitorB = new Keychain.Monitor("C:\\Users\\sundance\\keychain-data\\keychain-dotnet-b.db",
                ConfigFile, DropSqlFile, CreateSqlFile);

            //Assert.AreNotEqual(null, monitorB);




            // Seed the SPV wallets
            // Normally, you will display the mnemonics strings 
            // to the user for safe keeping


            int rcode = -1;
            string address;
            string[] mnemonics;
            logger.Info("Seeding database");
            rcode = gatewayA.seed(out address, out mnemonics);
            //Assert.AreEqual(0, rcode);

            rcode = gatewayB.seed(out address, out mnemonics);
            //Assert.AreEqual(0, rcode);




            // Create personas
            // name and subname are attributes which must be unique
            // for the sake of allowing users to easily differentiate
            // personas displayed in the UI

            {
                logger.Info("Creating personas");
                Persona personaA;
                gatewayA.getActivePersona(out personaA);

                if (personaA == null || personaA.isNull())
                {
                    logger.Info("Creating persona A");
                    string personaNameA = "DotNetTest";
                    string personaSubNameA = "A";
                    rcode = gatewayA.createPersona(out personaA, personaNameA, personaSubNameA, SecurityLevel.Medium);
                    //Assert.AreEqual(0, rcode);
                }
                else
                {
                    logger.Info("Persona A already exists. Skipping creation of new persona.");
                }

                Persona personaB;
                gatewayB.getActivePersona(out personaB);

                if (personaB == null || personaB.isNull())
                {
                    logger.Info("Creating persona B");
                    string personaNameB = "DotNetTest";
                    string personaSubNameB = "B";
                    rcode = gatewayB.createPersona(out personaB, personaNameB, personaSubNameB, SecurityLevel.Medium);
                    //Assert.AreEqual(0, rcode);
                }
                else
                {
                    logger.Info("Persona B already exists. Skipping creation of new persona.");
                }
            }


            logger.Info("Starting monitors.");
            monitorA.onStart();
            monitorA.onResume();

            monitorB.onStart();
            monitorB.onResume();


            // Wait until persona is confirmed on the blockchain, ie is mature

            // Here, the wait time between checks is arbitrary. Block times 
            // range from a few mintues to ten minutes, so any wait time will 
            // suffice. Most real applications are GUI-based hence there will
            // be no need for this type of waiting loop; just prevent the user
            // from seeing/doing anything with the persona in the main UI loop

            logger.Info("Waiting on both personas to mature");
            Persona activePersonaA, activePersonaB;
            gatewayA.getActivePersona(out activePersonaA);
            gatewayB.getActivePersona(out activePersonaB);

            while (!activePersonaA.isMature() || !activePersonaB.isMature())
            {
                gatewayA.getActivePersona(out activePersonaA);
                logger.Info("A root maturity: " + activePersonaA.getRootMaturity());
                logger.Info("A status: " + activePersonaA.getStatus());

                gatewayB.getActivePersona(out activePersonaB);
                logger.Info("B root maturity: " + activePersonaB.getRootMaturity());
                logger.Info("B status: " + activePersonaB.getStatus());
                Thread.Sleep(31000);
            }


            // NOTE: CHANGE THIS DOMAIN STRING TO A STRING OF YOUR CHOOSING
            string domain = "XXX-console-sample";
            ConsoleApp.Directory directory = new ConsoleApp.Directory(gatewayA, domain);
            directory.Start();

            // Test self-encrypt/verification with device A

            logger.Info("Testing self encryption/signing");
            const string clearTextA = "Got it A!!!";
            string cipherTextA, signedTextA;
            Contact[] contactsA = new Contact[] { };
            rcode = gatewayA.encrypt(out cipherTextA, clearTextA, contactsA);
            //Assert.AreEqual(0, rcode);

            string rClearTextA;
            rcode = gatewayA.decrypt(out rClearTextA, cipherTextA);
            //Assert.AreEqual(0, rcode);

            //Assert.AreEqual(rClearTextA, clearTextA);


            rcode = gatewayA.sign(out signedTextA, clearTextA);
            //Assert.AreEqual(0, rcode);

            List<Verification> results;
            rcode = gatewayA.verify(out results, out rClearTextA, signedTextA);
            //Assert.AreEqual(0, rcode);

            //Assert.AreEqual(rClearTextA, clearTextA);
            //Assert.AreEqual(1, results.Count);
            //Assert.AreEqual(true, results[0].verified);
            //Assert.AreEqual(personaA.getId(), results[0].facade.getId());


            rcode = gatewayA.signThenEncrypt(out cipherTextA, clearTextA, contactsA);
            ////Assert.AreEqual(0, rcode);

            rcode = gatewayA.decryptThenVerify(out results, out rClearTextA, cipherTextA);
            //Assert.AreEqual(0, rcode);
            //Assert.AreEqual(rClearTextA, clearTextA);
            //Assert.AreEqual(1, results.Count);
            //Assert.AreEqual(true, results[0].verified);
            //Assert.AreEqual(personaA.getId(), results[0].facade.getId());





            // Test self-encrypt/verification with device B

            string clearTextB = "Got it B!!!";
            string cipherTextB;
            Contact[] contactsB = new Contact[] { };
            rcode = gatewayB.encrypt(out cipherTextB, clearTextB, contactsB);
            //Assert.AreEqual(0, rcode);

            string rClearTextB;
            rcode = gatewayB.decrypt(out rClearTextB, cipherTextB);
            //Assert.AreEqual(0, rcode);

            //Assert.AreEqual(rClearTextB, clearTextB);


            rcode = gatewayB.decrypt(out rClearTextB, cipherTextA);
            //Assert.AreNotEqual(0, rcode);

            //Assert.AreNotEqual(rClearTextB, clearTextA);

            List<Verification> resultsB;
            rcode = gatewayB.verify(out resultsB, out rClearTextB, signedTextA);
            //Assert.AreNotEqual(0, rcode);

            //Assert.AreNotEqual(rClearTextB, signedTextA);

            rcode = gatewayB.sign(out cipherTextB, clearTextB/*, personaB*/);
            //Assert.AreEqual(0, rcode);

            rcode = gatewayB.verify(out resultsB, out rClearTextB, cipherTextB);
            //Assert.AreEqual(0, rcode);

            //Assert.AreEqual(rClearTextB, clearTextB);
            //Assert.AreEqual(1, resultsB.Count);
            //Assert.AreEqual(true, resultsB[0].verified);
            //Assert.AreEqual(personaB.getId(), resultsB[0].facade.getId());


            rcode = gatewayB.signThenEncrypt(out cipherTextB, clearTextB, contactsB);
            //Assert.AreEqual(0, rcode);

            rcode = gatewayB.decryptThenVerify(out resultsB, out clearTextB, cipherTextB);
            //Assert.AreEqual(0, rcode);
            //Assert.AreEqual(rClearTextB, clearTextB);
            //Assert.AreEqual(1, resultsB.Count);
            //Assert.AreEqual(true, resultsB[0].verified);
            //Assert.AreEqual(personaB.getId(), resultsB[0].facade.getId());



            // Pair
            logger.Info("Beginning pairing process");

            Contact contactA, contactB;
            rcode = gatewayA.createContact(out contactB, "Contact", "B", activePersonaB.getUri());
            //Assert.AreEqual(0, rcode);

            rcode = gatewayB.createContact(out contactA, "Contact", "A", activePersonaA.getUri());
            //Assert.AreEqual(0, rcode);



            // Get the contacts
            rcode = gatewayA.getContacts(out contactsA);
            //Assert.AreEqual(0, rcode);

            rcode = gatewayB.getContacts(out contactsB);
            //Assert.AreEqual(0, rcode);




            // Test encryption/verification to contacts

            rcode = gatewayA.encrypt(out cipherTextA, clearTextA, contactsA);
            //Assert.AreEqual(0, rcode);
            rcode = gatewayB.decrypt(out rClearTextB, cipherTextA);
            //Assert.AreEqual(0, rcode);

            //Assert.AreEqual(rClearTextB, clearTextA);

            rcode = gatewayB.signThenEncrypt(out cipherTextB, clearTextB, contactsB);
            //Assert.AreEqual(0, rcode);

            rcode = gatewayA.decryptThenVerify(out results, out rClearTextA, cipherTextB);
            //Assert.AreEqual(0, rcode);
            //Assert.AreEqual(rClearTextA, clearTextB);
            //Assert.AreEqual(1, results.Count);
            //Assert.AreEqual(true, results[0].verified);



            logger.Info("Sleeping while directory runs.");
            Thread.Sleep(23000);




            // Stop the directory and monitors
            directory.Stop();

            logger.Info("Done test.");
            logger.Info("Stopping monitor");
            monitorA.onPause();
            monitorA.onStop();

            monitorB.onPause();
            monitorB.onStop();
            logger.Info("Monitor stopped");



        }
    }
}
