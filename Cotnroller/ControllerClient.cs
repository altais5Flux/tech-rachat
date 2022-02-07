using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Objets100cLib;
using WebservicesSage.Object;
using WebservicesSage.Singleton;
using WebservicesSage.Utils;
using WebservicesSage.Utils.Enums;
using Newtonsoft.Json.Linq;

namespace WebservicesSage.Cotnroller
{
    public static class ControllerClient
    {
        /// <summary>
        /// Permets de remonter toute la base clients de SAGE vers Prestashop
        /// Ne remonte que les clients avec un mail 
        /// </summary>
        public static void SendAllClients()
        {
            var compta = SingletonConnection.Instance.Gescom.CptaApplication;
            var clientsSageObj = compta.FactoryFournisseur.List;

            var clients = GetListOfClientToProcess(clientsSageObj);

            int increm = 100 / clients.Count;

            foreach (Client client in clients)
            {
                // SingletonUI.Instance.LogBox.Invoke((MethodInvoker)(() => SingletonUI.Instance.LogBox.AppendText("-- Processing Client -- " + client.Intitule + " START -- " + Environment.NewLine)));
                /* foreach (ClientLivraisonAdress item in client.clientLivraisonAdresses)
                 {
                     SingletonUI.Instance.LogBox.Invoke((MethodInvoker)(() => SingletonUI.Instance.LogBox.AppendText("-- Processing Adresse -- " + item.Intitule + " -- " + Environment.NewLine)));
                 }*/
                if (UtilsConfig.ActiveClient.Equals("FALSE"))
                {
                    if (client.Sommeil)
                    {
                        continue;
                    }

                }
                string clientXML = UtilsSerialize.SerializeObject<Client>(client);

                UtilsWebservices.SendData(UtilsConfig.BaseUrl + EnumEndPoint.Client.Value, clientXML);

               // SingletonUI.Instance.ClientCircleProgress.Invoke((MethodInvoker)(() => SingletonUI.Instance.ClientCircleProgress.Value += increm));
                //SingletonUI.Instance.LogBox.Invoke((MethodInvoker)(() => SingletonUI.Instance.LogBox.AppendText("-- Processing Client -- " + client.Intitule + " END -- " + Environment.NewLine)));
            }

            MessageBox.Show("end client sync", "ok",
                               MessageBoxButtons.OK,
                               MessageBoxIcon.Information);

        }


        public static void SendClient(string ct_num)
        {
            try
            {
                var compta = SingletonConnection.Instance.Gescom.CptaApplication;
                var clientsSageObj = compta.FactoryFournisseur.ReadNumero(ct_num);

                var clients = GetClientToProcess(clientsSageObj);

                //int increm = 100 / clients.Count;

                foreach (Client client in clients)
                {
                    if (UtilsConfig.ActiveClient.Equals("FALSE"))
                    {
                        if (client.Sommeil)
                        {
                            continue;
                        }

                    }
                    string clientXML = UtilsSerialize.SerializeObject<Client>(client);

                    UtilsWebservices.SendData(UtilsConfig.BaseUrl + EnumEndPoint.Client.Value, clientXML);
                }

                MessageBox.Show("end client sync", "ok",
                                   MessageBoxButtons.OK,
                                   MessageBoxIcon.Information);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error",
                                  MessageBoxButtons.OK,
                                  MessageBoxIcon.Information);
            }
        }
        private static List<Client> GetClientToProcess(IBOFournisseur3 clientsSageObj)
        {
            List<Client> clientToProcess = new List<Client>();

            Client client = new Client(clientsSageObj);

            if (!HandleClientError(client))
            {
                client.setClientLivraisonAdresse();
                clientToProcess.Add(client);
            }
            return clientToProcess;
        }

        /// <summary>
        /// Permet de vérifier si un client comporte des erreur ou non
        /// </summary>
        /// <param name="client">Client à tester</param>
        /// <returns></returns>
        private static bool HandleClientError(Client client)
        {
            bool error = false;

            if(String.IsNullOrEmpty(client.Email))
            {
                error = true;
               // SingletonUI.Instance.LogBox.Invoke((MethodInvoker)(() => SingletonUI.Instance.LogBox.AppendText("Client :  " + client.Intitule + " No Mail Found" + Environment.NewLine)));


                // on affiche une erreur + log 
            }

            return error;
        }

        /// <summary>
        /// Permet de récupérer une liste de Client depuis une liste de Client SAGE
        /// </summary>
        /// <param name="clientsSageObj">List de client SAGE</param>
        /// <returns></returns>
        private static List<Client> GetListOfClientToProcess(IBICollection clientsSageObj)
        {
            List<Client> clientToProcess = new List<Client>();
            foreach (IBOFournisseur3 clientSageObj in clientsSageObj)
            {
                Client client = new Client(clientSageObj);

                if (!HandleClientError(client))
                {
                    client.setClientLivraisonAdresse();
                    clientToProcess.Add(client);
                }
            }
            return clientToProcess;
        }

        /// <summary>
        /// Permet de vérifier si un Client existe dans SAGE
        /// </summary>
        /// <param name="CT_num"></param>
        /// <returns></returns>
        public static bool CheckIfFournisseurCtNumExist(string CT_num)
        {
            if (String.IsNullOrEmpty(CT_num))
            {
                return false;
            }
            else
            {
                var compta = SingletonConnection.Instance.Gescom.CptaApplication;
                if (compta.FactoryFournisseur.ExistNumero(CT_num))
                {
                    return true;
                }
                else
                {
                    return false;
                }

            }

        }
        public static string CheckIfFournisseurEmailExist(string email)
        {
            foreach (IBOFournisseur3 fournisseur in SingletonConnection.Instance.Compta.FactoryFournisseur.List)
            {
                if (fournisseur.Telecom.EMail.ToUpper().Equals(email.ToUpper()))
                {
                    return  fournisseur.CT_Num;
                }
            }
            return "";
        }

        /// <summary>
        /// Permet de crée un Client dans la base SAGE depuis un objet json de prestashop
        /// </summary>
        /// <param name="jsonClient">json du Client à crée</param>
        /// <returns></returns>
        public static string CreateNewFournisseur(string jsonClient, JToken jsonOrder)
        {
            JObject customer = JObject.Parse(jsonClient);

            var compta = SingletonConnection.Instance.Gescom.CptaApplication;
            var gescom = SingletonConnection.Instance.Gescom;
            IBOFournisseur3 clientSage = (IBOFournisseur3)compta.FactoryFournisseur.Create();
            clientSage.SetDefault();

            if (jsonOrder["invoice_adresse1"].ToString().Length > 35)
            {
                clientSage.Adresse.Adresse = jsonOrder["invoice_adresse1"].ToString().Substring(0, 35);
            }
            else
            {
                clientSage.Adresse.Adresse = jsonOrder["invoice_adresse1"].ToString();
            }
            if (!String.IsNullOrEmpty(jsonOrder["invoice_adresse2"].ToString()))
            {
                clientSage.Adresse.Complement = jsonOrder["invoice_adresse2"].ToString();
            }
            if (!String.IsNullOrEmpty(jsonOrder["invoice_postcode"].ToString()))
            {
                clientSage.Adresse.CodePostal = jsonOrder["invoice_postcode"].ToString();
            }
            if (!String.IsNullOrEmpty(jsonOrder["invoice_city"].ToString()))
            {
                clientSage.Adresse.Ville = jsonOrder["invoice_city"].ToString();
            }
            if (!String.IsNullOrEmpty(jsonOrder["invoice_country"].ToString()))
            {
                clientSage.Adresse.Pays = jsonOrder["invoice_country"].ToString();
            }
            if (!String.IsNullOrEmpty(jsonOrder["invoice_phone"].ToString()))
            {
                clientSage.Telecom.Telephone = jsonOrder["invoice_phone"].ToString();
            }

            if (String.IsNullOrEmpty(UtilsConfig.PrefixClient))
            {
                // pas de configuration renseigner pour le prefix client
                // todo log
                int iterID = Int32.Parse(UtilsWebservices.SendDataNoParse(UtilsConfig.BaseUrl + EnumEndPoint.Client.Value, "getClientIterationSage&clientID="+ customer["id"].ToString()));
                while (compta.FactoryFournisseur.ExistNumero(iterID.ToString()))
                {
                    iterID++;
                }
                clientSage.CT_Num = iterID.ToString();
            }
            else
            {
                clientSage.CT_Num = UtilsConfig.PrefixClient + customer["id"].ToString();
            }


            string intitule = customer["firstname"].ToString().ToUpper() + " " + customer["lastname"].ToString().ToUpper();

            clientSage.Telecom.EMail = customer["email"].ToString();
            clientSage.CT_Intitule = intitule;
            if (intitule.Length > 17)
            {
                clientSage.CT_Classement = intitule.Substring(0, 17);
            }
            else
            {
                clientSage.CT_Classement = intitule;
            }

            clientSage.Write();

            if (jsonOrder["invoice_adresse1"].ToString().Length > 35)
            {
                clientSage.Adresse.Adresse = jsonOrder["invoice_adresse1"].ToString().Substring(0, 35);
            }
            else
            {
                clientSage.Adresse.Adresse = jsonOrder["invoice_adresse1"].ToString();
            }
            clientSage.Adresse.Complement = jsonOrder["invoice_adresse2"].ToString();
            clientSage.Adresse.CodePostal = jsonOrder["invoice_postcode"].ToString();
            clientSage.Adresse.Ville = jsonOrder["invoice_city"].ToString();
            clientSage.Adresse.Pays = jsonOrder["invoice_country"].ToString();
            if (String.IsNullOrEmpty(UtilsConfig.CondLivraison))
            {
                // pas de configuration renseigner pour CondLivraison par defaut
                // todo log
            }
            else
            {
                clientSage.ConditionLivraison = gescom.FactoryConditionLivraison.ReadIntitule(UtilsConfig.CondLivraison);
            }
            if (String.IsNullOrEmpty(UtilsConfig.Expedition))
            {
                // pas de configuration renseigner pour Expedition par defaut
                // todo log
            }
            else
            {
                clientSage.Expedition = gescom.FactoryExpedition.ReadIntitule(UtilsConfig.Expedition);
            }
            clientSage.Write();

            // on envoie une notification à préstashop pour lui informer de la créeation dans SAGE du client
            UtilsWebservices.SendDataNoParse(UtilsConfig.BaseUrl + EnumEndPoint.Client.Value, "updateCTnum&clientID=" + customer["id"].ToString() + "&ct_num=" + clientSage.CT_Num);
            UtilsWebservices.SendDataNoParse(UtilsConfig.BaseUrl + EnumEndPoint.Client.Value, "updateIter&iter=" + clientSage.CT_Num);


            return clientSage.CT_Num;
        }
    }
}
