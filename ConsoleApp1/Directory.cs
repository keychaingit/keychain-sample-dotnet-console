
//Directory class

// This class implements a simple directory client that enables a C# Gateway to pair using the hosted directory
// The directory client takes a gateway object and a domain string that labels the set of URIs that will be uploaded and retrieved from the server.
// 
// How to use:
// Instantiate the Directory with an existing gateway.
// Start the directory monitoring task by calling the Directory.Start() method.
// Stop the directory monitoring by calling the Directory.Stop() method.
// Whilst the directory monitoring task is runner, the Directory loops periodically (eg, every 23 seconds) to check 

// In the future, the Directory class will be made part of the Keychain dll.

using Keychain;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net;



namespace ConsoleApp // Choose any namespace you wish
{

    // Class for JSON parsing
    public class DirectoryGetResponse
    {
        public string response_code { get; set; }
        public DirectoryEntry[] results { get; set; }

    }


    public class DirectoryEntry
    {
        public string name { get; set; }
        public string encr_txid { get; set; }
        public string encr_vout { get; set; }
        public string sign_txid { get; set; }
        public string sign_vout { get; set; }

    }


    // The Directory client
    public class Directory
    {


        private Thread thread;
        private Gateway gateway;
        private bool shouldStop;
        private string domain;
        static readonly NLog.Logger logger = LogManager.GetCurrentClassLogger();
        private int sleepIntervalMillisecs = 13000;



        public Directory(Gateway g, string domain)
        {
            gateway = g;
            shouldStop = false;
            this.domain = domain;

        }



        // Start the thread
        public void Start()
        {
            logger.Debug("Directory for domain " + domain + " started.");

            shouldStop = false;

            thread = new Thread(CheckDirectory);
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
        }




        // Stop the thread
        public void Stop()
        {
            logger.Debug("Stopping directory for domain " + domain);
            shouldStop = true;
            if (thread.ThreadState != ThreadState.Unstarted)
            {
                thread.Join();
            }
        }






        // Main thread loop
        // This uploads the URI of the active persona
        // Then loops to download the URIs of devices on the specified domain and adds then as a contact
        // If they are not already in the set of contacts for the active persona
        private void CheckDirectory()
        {
            logger.Debug("Directory thread started.");
            int rcode = -1;
            if (shouldStop) return;


            string personaName = null;

            if (shouldStop) return;


            // find the current persona
            Persona foundPersona = null;

            try
            {
                gateway.getActivePersona(out foundPersona);
            }
            catch (Exception e)
            {
                logger.Error(e, "Error while getting active persona: " + e.Message);
                logger.Info("Exiting directory check thread due to previous error.");
                return;
            }

            if (foundPersona == null || foundPersona.isNull())
            {
                logger.Error("Unexpected null active persona.");
                logger.Info("Exiting directory check thread due to previous error.");
                return;
            }



            personaName = foundPersona.getName();
            string personaUrl = System.Text.Encoding.UTF8.GetString(foundPersona.getUri().serialize());
            try
            {
                string encr_url = personaUrl.Split(';')[0];
                string sign_url = personaUrl.Split(';')[1];

                string encr_txid = encr_url.Split(':')[0];
                int encr_vout = Int32.Parse(encr_url.Split(':')[1]);
                string sign_txid = sign_url.Split(':')[0];
                int sign_vout = Int32.Parse(sign_url.Split(':')[1]);

                using (var client = new WebClient())
                {
                    string jsonString = "{ \"name\": \"" + personaName + "\", " +
                        "\"encr_txid\": \"" + encr_txid + "\", " +
                        "\"encr_vout\": \"" + encr_vout + "\", " +
                        "\"sign_txid\": \"" + sign_txid + "\", " +
                        "\"sign_vout\": \"" + sign_vout + "\"} ";

                    client.Headers.Add(HttpRequestHeader.ContentType, "application/json");
                    string url = "http://54.65.160.194:3301/adsimulator/uploaduri/" + domain
                        + "/" + encr_txid
                        + "/" + encr_vout
                        + "/" + sign_txid
                        + "/" + sign_vout;
                    logger.Debug("Sending request to upload URI to the directory. URL: " + url);
                    string response = client.DownloadString(new System.Uri(url));

                    logger.Debug("Got response from AD simulator: " + response);
                    var jsonObject = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);
                    string return_string = jsonObject["response_code"];
                    if (return_string == "OK")
                    {
                        logger.Info("Blockchain id upload OK");
                    }
                    else if (return_string == "T003_PROFILE_EXISTS")
                    {
                        logger.Info("Blockchain id already uploaded");
                    }
                    else
                    {
                        logger.Warn("Blockchain id upload failed with error: " + return_string);
                    }
                }

            }
            catch (FormatException e)
            {
                object[] args = { personaUrl };
                logger.Fatal(e, "Failed to parse the URL of the found persona. Exiting setup. " + e.Message, args);
                return;
            }
            catch (Exception e)
            {
                logger.Error(e, "Error while sending request to AD simulator: " + e.Message);
                return;
            }



            // Loop indefinitely, pulling new URIs from the directory and adding then as contacts until flagged to stop
            while (!shouldStop)
            {
                Thread.Sleep(sleepIntervalMillisecs/2); // sleep the other half at the end of the loop

                // fetch uris from directory
                using (var client = new WebClient())
                {
                    //string return_string;
                    //List<string> uriStrings;

                    ConsoleApp.DirectoryGetResponse getResponse;
                    try
                    {
                        logger.Debug("Looking up blockchain ids for domain: " + domain);
                        string response = client.DownloadString(new System.Uri("http://54.65.160.194:3301/adsimulator/getalluri/" + domain));
                        getResponse = JsonConvert.DeserializeObject<ConsoleApp.DirectoryGetResponse>(response); 
                    }
                    catch (FormatException e)
                    {
                        object[] args = { personaUrl };
                        logger.Fatal(e, "Failed to parse the URL of the found persona. Exiting setup. " + e.Message, args);
                        return;
                    }
                    catch (Exception e)
                    {
                        logger.Error(e, "Error while sending GETALLURI request to AD simulator: " + e.Message);
                        return;
                    }

                    // loop for each found Uri
                    foreach (DirectoryEntry entry in getResponse.results)
                    {

                        //logger.Debug("Retrieved directory entry string from directory: " + uriStringJson);
                        if (getResponse.response_code == "OK")
                        {

                            string name = entry.name; //uriJsonObject["name"];
                            string uriString = entry.encr_txid + ":"
                                + entry.encr_vout + ";"
                                + entry.sign_txid + ":"
                                + entry.sign_vout;

                            logger.Debug("Retrieved URI string from directory: " + uriString);
                            Keychain.Uri uri = new Keychain.Uri(Encoding.UTF8.GetBytes(uriString));


                            // if found Uri is already in contacts or personas, skip
                            Persona[] personas = null;
                            try
                            {
                                logger.Debug("Getting personas to check whether URI is an existing persona.");
                                lock (gateway)
                                {
                                    rcode = gateway.getPersonas(out personas);
                                }
                                if (rcode != 0)
                                {
                                    logger.Error("Error while getting personas: " + rcode);
                                }

                            }
                            catch (Exception e)
                            {
                                logger.Error(e, "Exception while getting personas: " + e.Message);
                            }

                            bool matchedPersona = false;
                            foreach (Persona persona in personas)
                            {
                                if (Encoding.Unicode.GetString(persona.getUri().serialize()) == Encoding.Unicode.GetString(uri.serialize()))
                                {
                                    matchedPersona = true;
                                }
                            }

                            if (matchedPersona)
                            {
                                logger.Debug("Retrieved URI matched that of an existing persona. Skipping contact add op for this URI.");
                                continue;
                            }


                            // if found Uri is already in contacts or personas, skip
                            Contact[] contacts = null;
                            try
                            {
                                logger.Debug("Getting contacts to check whether URI is an existing contact.");
                                lock (gateway)
                                {
                                    rcode = gateway.getContacts(out contacts);
                                }
                                if (rcode != 0)
                                {
                                    logger.Error("Error while getting contacts: " + rcode);
                                }

                            }
                            catch (Exception e)
                            {
                                logger.Error(e, "Exception while getting contacts: " + e.Message);
                            }

                            bool matchedContact = false;
                            foreach (Contact contact in contacts)
                            {
                                if (Encoding.Unicode.GetString(contact.getUri().serialize()) == Encoding.Unicode.GetString(uri.serialize()))
                                {
                                    matchedContact = true;
                                }
                            }

                            if (matchedContact)
                            {
                                logger.Debug("Retrieved URI matched that of an existing contact. Skipping contact add op for this URI.");
                                continue;
                            }



                            // create contact from id
                            Contact newContact;
                            logger.Debug("Adding URI as a new contact.");

                            try
                            {

                                lock (gateway)
                                {
                                    rcode = gateway.createContact(out newContact, name, "", uri);
                                }

                                if (rcode != 0)
                                {
                                    logger.Error("Error during create contact: " + rcode);
                                    return;
                                }
                                else
                                {
                                    logger.Debug("Added contact: " + name);
                                }
                            }
                            catch (Exception e)
                            {
                                logger.Error(e, "Exception while creating contact: " + e.Message);
                                return;
                            }
                        }
                        else
                        {
                            logger.Warn("Blockchain id lookup for domain: " + domain + " failed with response: " + getResponse.response_code);
                            return;
                        }

                    }
                }

                if (shouldStop)
                {
                    break;
                }
                Thread.Sleep(sleepIntervalMillisecs / 2);

            }

            logger.Info("Recieved flag to stop Directory thread loop. Stopping.");
        }
    }

}
