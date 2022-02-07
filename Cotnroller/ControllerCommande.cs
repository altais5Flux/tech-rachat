using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using WebservicesSage.Utils;
using WebservicesSage.Utils.Enums;
using WebservicesSage.Singleton;
using Objets100cLib;
using WebservicesSage.Object;
using WebservicesSage.Object.DBObject;
using System.Windows.Forms;
using LiteDB;
using System.IO;
using System.Configuration;
using System.Data.SqlClient;

namespace WebservicesSage.Cotnroller
{
    public static class ControllerCommande
    {

        /// <summary>
        /// Lance le service de check des nouvelles commandes prestashop
        /// Définir le temps de passage de la tâche dans la config
        /// </summary>
        public static void LaunchService()
        {
            // SingletonUI.Instance.LogBox.Invoke((MethodInvoker)(() => SingletonUI.Instance.LogBox.AppendText("Commande Services Launched " + Environment.NewLine)));
            
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Elapsed += new ElapsedEventHandler(CheckForNewOrderCron);
            timer.Interval = UtilsConfig.CronTaskCheckForNewOrder;
            timer.Enabled = true;

            //timer for the restart
            System.Timers.Timer timerRestartApplication = new System.Timers.Timer();
            timerRestartApplication.Elapsed += new ElapsedEventHandler(RestartAPP);
            timerRestartApplication.Interval = UtilsConfig.CronTaskRestart;
            timerRestartApplication.Enabled = true;


            /*
            System.Timers.Timer timerUpdateStatut = new System.Timers.Timer();
            timerUpdateStatut.Elapsed += new ElapsedEventHandler(UpdateStatuOrder);
            timerUpdateStatut.Interval = UtilsConfig.CronTaskUpdateStatut;
            timerUpdateStatut.Enabled = true;*/
        }
        public static void RestartAPP(object source, ElapsedEventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("date de redémarrage : " + DateTime.Now + Environment.NewLine);
            System.IO.File.AppendAllText("Log\\restart.txt", sb.ToString());
            sb.Clear();
            System.Diagnostics.Process.Start(Application.ExecutablePath);
            Application.ExitThread();
            Application.Exit();
            Environment.Exit(200);
        }


        /// <summary>
        /// Event levé par une nouvelle commande dans prestashop
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public static void CheckForNewOrderCron(object source, ElapsedEventArgs e)
        {

            string currentIdOrder = "0";
            try
            {
                string response = UtilsWebservices.SendData(UtilsConfig.BaseUrl + EnumEndPoint.Commande.Value, "checkOrder");

                if (!response.Equals("none") && !response.Equals("[]"))
                {
                    
                    JArray orders = JArray.Parse(response);
                    
                    foreach (var order in orders)
                    {
                        if (string.IsNullOrEmpty(CheckifOrderExist(order["reference"].ToString())))
                        {
                            currentIdOrder = order["id_order"].ToString();
                            if (ControllerClient.CheckIfFournisseurCtNumExist(order["ALTAIS_CT_NUM"].ToString()))
                            {
                                // si le client existe on associé la commande à son compte
                                AddNewOrderForCustomer(order, order["ALTAIS_CT_NUM"].ToString());
                            }
                            else if (!String.IsNullOrEmpty(ControllerClient.CheckIfFournisseurEmailExist(order["email_customer"].ToString())))
                            {
                                AddNewOrderForCustomer(order, order["ALTAIS_CT_NUM"].ToString());
                            }
                            else
                            {
                                // si le client n'existe pas on récupère les info de presta et on le crée dans la base sage 
                                string client = UtilsWebservices.SendDataNoParse(UtilsConfig.BaseUrl + EnumEndPoint.Client.Value, "getClient&clientID=" + order["id_customer"]);
                                string ct_num = ControllerClient.CreateNewFournisseur(client, order);

                                if (!String.IsNullOrEmpty(ct_num))
                                {
                                    // le client à bien été crée on peut intégrer la commande sur son compte sage
                                    AddNewOrderForCustomer(order, ct_num);
                                }
                            }
                        }
                        else
                        {
                            StringBuilder sb = new StringBuilder();
                            sb.Append(DateTime.Now  + Environment.NewLine);
                            sb.Append(DateTime.Now + Environment.NewLine);
                            sb.Append("Commande "+ order["ALTAIS_CT_NUM"].ToString()+" exist déja");
                            File.AppendAllText("Log\\order.txt", sb.ToString());
                            sb.Clear();
                        }

                    }
                }
            }
            catch (Exception s)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(DateTime.Now+ s.Message + Environment.NewLine);
                sb.Append(DateTime.Now + s.StackTrace + Environment.NewLine);
                UtilsMail.SendErrorMail(DateTime.Now + s.Message + Environment.NewLine + s.StackTrace + Environment.NewLine, "COMMANDE");
                File.AppendAllText("Log\\order.txt", sb.ToString());
                sb.Clear();
                UtilsWebservices.SendDataNoParse(UtilsConfig.BaseUrl + EnumEndPoint.Commande.Value, "errorOrder&orderID=" + currentIdOrder);
            }

        }

        public static string CheckifOrderExist(string reference)
        {
            string reff = "TR " + reference;
            string SQL = null;
            string data = null;
            SQL = "SELECT  [DO_Ref] FROM [" + ConfigurationManager.AppSettings["DBNAME"].ToString() + "].[dbo].[F_DOCENTETE] WHERE DO_Ref= '"+ reff +"'";
            SqlDataReader dataReader = DB.Select(SQL);
            while (dataReader.Read())
            {
                data = dataReader.GetValue(0).ToString();
            }
            DB.Disconnect();

            return data;

        }


        public static void CheckForNewOrder()
        {

            string currentIdOrder = "0";
            try
            {
                string response = UtilsWebservices.SendData(UtilsConfig.BaseUrl + EnumEndPoint.Commande.Value, "checkOrder");

                if (!response.Equals("none") && !response.Equals("[]"))
                {

                    JArray orders = JArray.Parse(response);

                    foreach (var order in orders)
                    {

                        if (string.IsNullOrEmpty(CheckifOrderExist(order["reference"].ToString())))
                        {
                            currentIdOrder = order["id_order"].ToString();
                            if (ControllerClient.CheckIfFournisseurCtNumExist(order["ALTAIS_CT_NUM"].ToString()))
                            {
                                // si le client existe on associé la commande à son compte
                                AddNewOrderForCustomer(order, order["ALTAIS_CT_NUM"].ToString());
                            }
                            else if (!String.IsNullOrEmpty(ControllerClient.CheckIfFournisseurEmailExist(order["email_customer"].ToString())))
                            {
                                AddNewOrderForCustomer(order, order["ALTAIS_CT_NUM"].ToString());
                            }
                            else
                            {
                                // si le client n'existe pas on récupère les info de presta et on le crée dans la base sage 
                                string client = UtilsWebservices.SendDataNoParse(UtilsConfig.BaseUrl + EnumEndPoint.Client.Value, "getClient&clientID=" + order["id_customer"]);
                                string ct_num = ControllerClient.CreateNewFournisseur(client, order);

                                if (!String.IsNullOrEmpty(ct_num))
                                {
                                    // le client à bien été crée on peut intégrer la commande sur son compte sage
                                    AddNewOrderForCustomer(order, ct_num);
                                }
                            }
                        }
                        else
                        {
                            StringBuilder sb = new StringBuilder();
                            sb.Append(DateTime.Now + Environment.NewLine);
                            sb.Append(DateTime.Now + Environment.NewLine);
                            sb.Append("Commande " + order["ALTAIS_CT_NUM"].ToString() + " exist déja");
                            File.AppendAllText("Log\\order.txt", sb.ToString());
                            sb.Clear();
                        }
                    }
                }
            }
            catch (Exception s)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(DateTime.Now + s.Message + Environment.NewLine);
                sb.Append(DateTime.Now + s.StackTrace + Environment.NewLine);
                UtilsMail.SendErrorMail(DateTime.Now + s.Message + Environment.NewLine + s.StackTrace + Environment.NewLine, "COMMANDE");
                File.AppendAllText("Log\\order.txt", sb.ToString());
                sb.Clear();
                UtilsWebservices.SendDataNoParse(UtilsConfig.BaseUrl + EnumEndPoint.Commande.Value, "errorOrder&orderID=" + currentIdOrder);
            }

        }

        private static void UpdateStatuOrder(object source, ElapsedEventArgs e)
        {
            var gescom = SingletonConnection.Instance.Gescom;
            var compta = SingletonConnection.Instance.Compta;
            
            IBICollection AllOrders = gescom.FactoryDocumentVente.List;
            using (var db = new LiteDatabase(@"MyData.db"))
            {
                string prestaStatutId,orderStatutId,statut1, statut2, statut3;
                string[] prestaID, orderStatut;
                UtilsConfig.PrestaStatutId.TryGetValue("default", out prestaStatutId);
                prestaID = prestaStatutId.Split('_');
                UtilsConfig.OrderMapping.TryGetValue("default", out orderStatutId);
                orderStatut = orderStatutId.Split('_');
                statut1 = orderStatut[0];
                statut2 = orderStatut[1];
                statut3 = orderStatut[2];
                // Get a collection (or create, if doesn't exist)
                var col = db.GetCollection<LinkedCommandeDB>("Commande");
                foreach (LinkedCommandeDB item in col.FindAll())
                {
                    foreach (IBODocumentVente3 order in AllOrders)
                    {
                        if (order.DO_Ref.Equals(item.DO_Ref))
                        {
                            if (!(order.DO_Type.ToString().Equals(item.OrderType)))
                            {
                                if (order.DO_Type.ToString().Equals("DocumentType.DocumentTypeVenteCommande"))
                                {
                                    break;
                                }
                                else
                                {
                                    if (order.DO_Type.ToString().Equals(statut1))
                                    {
                                        UtilsWebservices.SendDataNoParse(UtilsConfig.BaseUrl + EnumEndPoint.Commande.Value, "updateOrder&orderID=" + item.OrderID + "&orderType=" + prestaID[0]);
                                        item.OrderType = statut1;
                                        col.Update(item);
                                        break;
                                    }
                                    if (order.DO_Type.ToString().Equals(statut2))
                                    {
                                        UtilsWebservices.SendDataNoParse(UtilsConfig.BaseUrl + EnumEndPoint.Commande.Value, "updateOrder&orderID=" + item.OrderID + "&orderType=" + prestaID[1]);
                                        item.OrderType = statut2;
                                        col.Update(item);
                                        break;
                                    }
                                    if (order.DO_Type.ToString().Equals(statut3))
                                    {
                                        UtilsWebservices.SendDataNoParse(UtilsConfig.BaseUrl + EnumEndPoint.Commande.Value, "updateOrder&orderID=" + item.OrderID + "&orderType=" + prestaID[2]);
                                        col.Delete(item.Id);
                                        break;
                                    }
                                }
                                
                                

                                /*
                                switch (order.DO_Type.ToString())
                                {
                                    case "DocumentType.DocumentTypeVenteCommande":
                                        break;
                                    case statut1:
                                        UtilsWebservices.SendDataNoParse(UtilsConfig.BaseUrl + EnumEndPoint.Commande.Value, "updateOrder&orderID=" + item.OrderID + "&orderType="+prestaID[0]);
                                        item.OrderType = DocumentType.DocumentTypeVentePrepaLivraison;
                                        col.Update(item);
                                        break;
                                    case DocumentType.DocumentTypeVenteLivraison:
                                        UtilsWebservices.SendDataNoParse(UtilsConfig.BaseUrl + EnumEndPoint.Commande.Value, "updateOrder&orderID=" + item.OrderID + "&orderType=" + prestaID[1]);
                                        item.OrderType = DocumentType.DocumentTypeVenteLivraison;
                                        col.Update(item);
                                        break;
                                    case DocumentType.DocumentTypeVenteFacture:
                                        UtilsWebservices.SendDataNoParse(UtilsConfig.BaseUrl + EnumEndPoint.Commande.Value, "updateOrder&orderID=" + item.OrderID + "&orderType=" + prestaID[2]);
                                                col.Delete(item.Id);
                                        break;
                                    default:
                                        break;
                                }*/
                            }
                            
                        }
                    }
                }
            }
        }

                

        /// <summary>
        /// Crée une nouvelle commande pour un utilisateur
        /// </summary>
        /// <param name="jsonOrder">Order à crée</param>
        /// <param name="CT_Num">Client</param>
        public static void  AddNewOrderForCustomer(JToken jsonOrder, string CT_Num)
        {
            var gescom = SingletonConnection.Instance.Gescom;
            
            // création de l'entête de la commande 

            IBOFournisseur3 customer = gescom.CptaApplication.FactoryFournisseur.ReadNumero(CT_Num);
            try
            {
                if (!String.IsNullOrEmpty(jsonOrder["IBAN"].ToString()) && !String.IsNullOrEmpty(jsonOrder["BIC"].ToString()))
                {
                    string SQL = null;
                    string data = null;
                    SQL = "SELECT  MAX(BT_Num) FROM [" + ConfigurationManager.AppSettings["DBNAME"].ToString() + "].[dbo].[F_BANQUET] WHERE CT_Num= '"+customer.CT_Num +"'";
                    SqlDataReader dataReader = DB.Select(SQL);
                    while (dataReader.Read())
                    {
                        data = dataReader.GetValue(0).ToString();
                    }
                    DB.Disconnect();
                    if (!String.IsNullOrEmpty(data))
                    {
                        int num = int.Parse(data);
                        num++;
                        DB.Insert(customer.CT_Num, num, jsonOrder["IBAN"].ToString(), jsonOrder["BIC"].ToString(), jsonOrder["invoice_country"].ToString());
                    }
                    else
                    {
                        DB.Insert(customer.CT_Num, 1, jsonOrder["IBAN"].ToString(), jsonOrder["BIC"].ToString(), jsonOrder["invoice_country"].ToString());
                    }
                    DB.Disconnect();
                }
            }
            catch (Exception ec)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(DateTime.Now + ec.Message + Environment.NewLine);
                sb.Append(DateTime.Now + ec.StackTrace + Environment.NewLine);
                sb.Append(DateTime.Now + " Erreur avec IBAN ou BIC pour le fournisseur : " + customer.CT_Num + Environment.NewLine);
                File.AppendAllText("Log\\Banque.txt", sb.ToString());
                sb.Clear();
            }
            
            IBODocumentAchat3 order = gescom.FactoryDocumentAchat.CreateType(DocumentType.DocumentTypeAchatCommandeConf);
            order.SetDefault();
            order.SetDefaultFournisseur(customer);
            //Get date from Prestashop
            order.DO_Date = DateTime.Now;
            /*try
            {
               // string carrier_id = jsonOrder["order_carriere"].ToString();
                //order.Expedition = gescom.FactoryExpedition.ReadIntitule(UtilsConfig.OrderCarrierMapping[carrier_id]);
            }
            catch (Exception s){
                StringBuilder sb = new StringBuilder();
                sb.Append(DateTime.Now + s.Message + Environment.NewLine);
                sb.Append(DateTime.Now + s.StackTrace + Environment.NewLine);
                UtilsMail.SendErrorMail(DateTime.Now + s.Message + Environment.NewLine + s.StackTrace + Environment.NewLine, "TRANSPORTEUR");
                File.AppendAllText("Log\\order.txt", sb.ToString());
                sb.Clear();
            }*/
            order.Souche = gescom.FactorySoucheAchat.ReadIntitule(UtilsConfig.Souche);
            order.DO_Ref = "TR " + jsonOrder["reference"].ToString();
            order.SetDefaultDO_Piece();
                      
            order.Write();

            // set depot pour les commande de prestashop

            //order.Write();
            //order.CompteA.
            try
            {
                DB.InsertRachatEntete(ConfigurationManager.AppSettings["AFFAIRES"],order.DO_Piece);
                /*
                foreach (IBOCompteA3 item in SingletonConnection.Instance.Compta.FactoryAnalytique.List)
                {
                    if (item.CA_Num.Equals(ConfigurationManager.AppSettings["AFFAIRES"]))
                    {
                        order.CompteA = item;
                        order.Write();
                    }
                }*/
            }
            catch (Exception exce)
            {
                
                StringBuilder sb = new StringBuilder();
                sb.Append(DateTime.Now + " Erreur avec le codes d'affaires " + Environment.NewLine);
                sb.Append(DateTime.Now + exce.Message + Environment.NewLine);
                sb.Append(DateTime.Now + exce.StackTrace + Environment.NewLine);
                File.AppendAllText("Log\\order.txt", sb.ToString());
                sb.Clear();
            }
            
            // création des lignes de la commandes
            try
            {
                foreach (JToken product in jsonOrder["products"].Children())
                {
                    // on récupère la chaine de gammages d'un produit
                    /*string product_attribut_string = product["product_attribute_id_string"].ToString();
                    String[] subgamme = product_attribut_string.Split('|');


                    

                    if (subgamme.Length == 2)
                    {
                        // produit à simple gamme
                        IBOArticleGammeEnum3 articleEnum = ControllerArticle.GetArticleGammeEnum1(article, new Gamme(subgamme[0], subgamme[1]));
                        docLigne.SetDefaultArticleMonoGamme(articleEnum, Int32.Parse(product["product_quantity"].ToString()));
                    }
                    else if (subgamme.Length == 4)
                    {
                        // produit à double gamme
                        IBOArticleGammeEnum3 articleEnum = ControllerArticle.GetArticleGammeEnum1(article, new Gamme(subgamme[0], subgamme[1], subgamme[2], subgamme[3]));
                        IBOArticleGammeEnum3 articleEnum2 = ControllerArticle.GetArticleGammeEnum2(article, new Gamme(subgamme[0], subgamme[1], subgamme[2], subgamme[3]));
                        docLigne.SetDefaultArticleDoubleGamme(articleEnum, articleEnum2, Int32.Parse(product["product_quantity"].ToString()));
                    }
                    else
                    {*/
                    //TODO mapping transporteur else use default transporter

                    // produit simple
                    IBODocumentLigne3 docLigne = (IBODocumentLigne3)order.FactoryDocumentLigne.Create();

                    IBOArticle3 article = gescom.FactoryArticle.ReadReference(product["product_ref"].ToString());
                    docLigne.DL_PrixUnitaire = double.Parse(product["price"].ToString(), System.Globalization.CultureInfo.InvariantCulture);
                    docLigne.SetDefaultArticle(gescom.FactoryArticle.ReadReference(product["product_ref"].ToString()), Int32.Parse(product["product_quantity"].ToString()));
                        if (product["product_ref"].ToString().Equals(UtilsConfig.DefaultTransportReference.ToString()))
                        {
                            docLigne.DL_PrixUnitaire = Convert.ToDouble(product["price"].ToString().Replace('.', ','));
                            //ajout message transporteur
                            docLigne.TxtComplementaire = jsonOrder["message"].ToString();
                        }
                        else if (product["product_ref"].ToString().Equals("REMISE"))
                        {
                            docLigne.DL_PrixUnitaire = Convert.ToDouble(product["price"].ToString().Replace('.', ','));
                        }
                    //}

                    docLigne.Write();
                }
                DB.InsertRachatLigne(ConfigurationManager.AppSettings["AFFAIRES"], order.DO_Piece);
            }
            catch(Exception e)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(DateTime.Now + e.Message + Environment.NewLine);
                sb.Append(DateTime.Now + e.StackTrace + Environment.NewLine);
                UtilsMail.SendErrorMail(DateTime.Now + e.Message + Environment.NewLine + e.StackTrace + Environment.NewLine, "COMMANDE LIGNE");
                File.AppendAllText("Log\\order.txt", sb.ToString());
                sb.Clear();
                UtilsWebservices.SendDataNoParse(UtilsConfig.BaseUrl + EnumEndPoint.Commande.Value, "errorOrder&orderID=" + jsonOrder["id_order"]);
                order.Remove();
                return;
            }
            UtilsWebservices.SendDataNoParse(UtilsConfig.BaseUrl + EnumEndPoint.Commande.Value, "validateOrder&orderID=" + jsonOrder["id_order"]);
            addOrderToLocalDB(jsonOrder["id_order"].ToString(), order.Fournisseur.CT_Num, order.DO_Piece, order.DO_Ref);
            // on notfie prestashop que la commande à bien été crée dans SAGE
            
        }

        private static void addOrderToLocalDB(string orderID, string CT_Num, string DO_piece, string DO_Ref)
        {
            // Open database (or create if doesn't exist)
            using (var db = new LiteDatabase(@"MyData.db"))
            {
                // Get a collection (or create, if doesn't exist)
                var col = db.GetCollection<LinkedCommandeDB>("Commande");

                // Create your new customer instance
                var commande = new LinkedCommandeDB
                {
                    OrderID = orderID,
                    OrderType = "DocumentType.DocumentTypeVenteCommande",
                    CT_Num = CT_Num,
                    DO_piece = DO_piece,
                    DO_Ref = DO_Ref

                };
                col.Insert(commande);
            }
        }

        public static string GetPrestaOrderStatutFromMapping(DocumentType orderSageType= DocumentType.DocumentTypeVenteCommande, string prestaStatutId="")
        {
            Array x = UtilsConfig.OrderMapping.ToArray();
            foreach (KeyValuePair<string, string> kvp in UtilsConfig.OrderMapping.ToArray())
            {
                string[] res = kvp.Value.ToString().Split('_');
                if (res[1].Equals(prestaStatutId))
                {
                    return res[0].ToString();
                }
            }
            return "";
        }

        public static void UpdateStatutOnPresta(string orderID, string newType)
        {
            UtilsWebservices.SendDataNoParse(UtilsConfig.BaseUrl + EnumEndPoint.Commande.Value, "updateOrder&orderID=" + orderID + "&orderType=" + newType);
        }
    }
}
