using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebservicesSage.Object
{
    class DB
    {
        private static SqlConnection cnn;
        private static void Connect()
        {
            string connetionString;
            connetionString = @"Data Source=" + ConfigurationManager.AppSettings["SERVER"].ToString() + ";Initial Catalog=" + ConfigurationManager.AppSettings["DBNAME"].ToString() + ";User ID=" + ConfigurationManager.AppSettings["SQLUSER"].ToString() + ";Password=" + ConfigurationManager.AppSettings["SQLPWD"].ToString();
            cnn = new SqlConnection(connetionString);
            cnn.Open();
        }

        public static void Disconnect()
        {
            cnn.Close();
        }

        public static SqlDataReader Select(string sql)
        {
            Connect();

            SqlCommand command = new SqlCommand(sql, cnn);
            SqlDataReader dataReader = command.ExecuteReader();

            //Disconnect();
            return dataReader;
        }
        public static void Insert(string ct_num, int cb_num,string iban,string bic, string pays)
        {
            try
            {
                Connect();
                var sql = "Insert into " + ConfigurationManager.AppSettings["DBNAME"].ToString() + ".dbo.F_BANQUET (CT_Num,BT_Num, BT_Intitule, BT_IBAN, BT_BIC, BT_Struct,BT_Compte,N_Devise,BT_Pays) " +
                    "values(@ct_num,@cb_num,@bank,@iban,@bic,@struct,@bt_compte,@devise,@pays)";
               // Connect();
                using (SqlCommand command = new SqlCommand(sql, cnn))
                {
                    command.Parameters.Add("@ct_num", SqlDbType.VarChar).Value = ct_num;
                    command.Parameters.Add("@cb_num", SqlDbType.Int).Value = cb_num;
                    command.Parameters.Add("@iban", SqlDbType.VarChar).Value = iban;
                    command.Parameters.Add("@bt_compte", SqlDbType.VarChar).Value = iban;
                    command.Parameters.Add("@bic", SqlDbType.VarChar).Value = bic;
                    command.Parameters.Add("@bank", SqlDbType.VarChar).Value = "bank";
                    command.Parameters.Add("@struct", SqlDbType.Int).Value = 3;
                    command.Parameters.Add("@devise", SqlDbType.Int).Value = ConfigurationManager.AppSettings["DEVISE"].ToString();
                    if (!string.IsNullOrEmpty(pays))
                    {
                        pays = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(pays.ToLower()); ;
                        command.Parameters.Add("@pays", SqlDbType.VarChar).Value = pays.ToUpper();
                    }

                    command.ExecuteNonQuery();
                    Disconnect();
                }
            }
            catch (Exception s)
            {
                Disconnect();
                StringBuilder sb = new StringBuilder();
                sb.Append(DateTime.Now + s.Message + Environment.NewLine);
                sb.Append(DateTime.Now + s.StackTrace + Environment.NewLine);
                sb.Append(DateTime.Now + " Erreur avec IBAN ou BIC pour le fournisseur : " + ct_num + Environment.NewLine);
                File.AppendAllText("Log\\Banque.txt", sb.ToString());
                sb.Clear();
            }

            try
            {
                Connect();
                var sql = "UPDATE " + ConfigurationManager.AppSettings["DBNAME"].ToString() + ".dbo.F_COMPTET set BT_Num = @cb_num where CT_Num = @ct_num";

                using (SqlCommand fournisseur = new SqlCommand(sql, cnn))
                {
                    fournisseur.Parameters.Add("@ct_num", SqlDbType.VarChar).Value = ct_num;
                    fournisseur.Parameters.Add("@cb_num", SqlDbType.Int).Value = cb_num;
                    fournisseur.ExecuteNonQuery();
                    Disconnect();
                }

            }
            catch(Exception s)
            {
                Disconnect();
                StringBuilder sb = new StringBuilder();
                sb.Append(DateTime.Now + s.Message + Environment.NewLine);
                sb.Append(DateTime.Now + s.StackTrace + Environment.NewLine);
                sb.Append(DateTime.Now + " Erreur avec la définition de la banque comme banque par default pour le fournisseur : " + ct_num + Environment.NewLine);
                File.AppendAllText("Log\\Banque.txt", sb.ToString());
                sb.Clear();
            }

        }
        public static void InsertRachatEntete(string Code_affaire, string do_piece)
        {
            try
            {
                Connect();
                var sql = "update " + ConfigurationManager.AppSettings["DBNAME"].ToString() + ".dbo.F_DOCENTETE set CA_Num =@Code_affaire where DO_Piece=@do_piece";
                // Connect();
                using (SqlCommand command = new SqlCommand(sql, cnn))
                {
                    command.Parameters.Add("@Code_affaire", SqlDbType.VarChar).Value = Code_affaire;
                    command.Parameters.Add("@do_piece", SqlDbType.VarChar).Value = do_piece;

                    command.ExecuteNonQuery();
                    Disconnect();
                }

            }
            catch (Exception s)
            {
                Disconnect();
                StringBuilder sb = new StringBuilder();
                sb.Append(DateTime.Now + s.Message + Environment.NewLine);
                sb.Append(DateTime.Now + s.StackTrace + Environment.NewLine);
                sb.Append(DateTime.Now + " Erreur avec code d'affaire dans entete document: " + do_piece + Environment.NewLine);
                File.AppendAllText("Log\\order.txt", sb.ToString());
                sb.Clear();
            }

        }
        public static void InsertRachatLigne(string Code_affaire, string do_piece)
        {
            try
            {
                Connect();
                var sql = "update " + ConfigurationManager.AppSettings["DBNAME"].ToString() + ".dbo.F_DOCLIGNE set CA_Num =@Code_affaire where DO_Piece=@do_piece";
                // Connect();
                using (SqlCommand command = new SqlCommand(sql, cnn))
                {
                    command.Parameters.Add("@Code_affaire", SqlDbType.VarChar).Value = Code_affaire;
                    command.Parameters.Add("@do_piece", SqlDbType.VarChar).Value = do_piece;

                    command.ExecuteNonQuery();
                    Disconnect();
                }

            }
            catch (Exception s)
            {
                Disconnect();
                StringBuilder sb = new StringBuilder();
                sb.Append(DateTime.Now + s.Message + Environment.NewLine);
                sb.Append(DateTime.Now + s.StackTrace + Environment.NewLine);
                sb.Append(DateTime.Now + " Erreur avec code d'affaire dans lignes document: " + do_piece + Environment.NewLine);
                File.AppendAllText("Log\\order.txt", sb.ToString());
                sb.Clear();
            }

        }


    }
}
