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



namespace ConsoleApp
{
    public class Monitor
    {


        private Thread thread;
        private Gateway gateway;
        private bool shouldStop;
        //private List<string> emailAddresses;
        static readonly NLog.Logger logger = LogManager.GetCurrentClassLogger();




        public Monitor(Gateway g)
        {
            gateway = g;
            shouldStop = false;
            //this.emailAddresses = eAddresses;

        }




        public void Start()
        {
            logger.Trace("in Start()");

            shouldStop = false;

            thread = new Thread(DoIt);
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
        }





        public void Stop()
        {
            shouldStop = true;
            if (thread.ThreadState != ThreadState.Unstarted)
            {
                thread.Join();
            }
        }







        private void DoIt()
        {

            int rcode = -1;
            if (shouldStop) return;


            Persona[] personas = null;
            string personaName = null;

            if (shouldStop) return;

            try
            {
                logger.Debug("Getting personas");
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

            // find the current persona
            //Persona foundPersona = null;

            foreach (Persona foundPersona in personas)
            {
                personaName = foundPersona.getName();
                string personaUrl = foundPersona.getUri().ToString();
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
                        string response = client.UploadString(new System.Uri("http://54.65.160.194:3301/adsimulator/set"), "POST", jsonString);

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
            }
        }
    }

}
