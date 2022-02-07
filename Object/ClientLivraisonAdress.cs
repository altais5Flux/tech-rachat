using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Objets100cLib;


namespace WebservicesSage.Object
{
    [Serializable()]
    public class ClientLivraisonAdress
    {
        public string Intitule { get; set; }
        public string Adresse { get; set; }
        public string Complement { get; set; }
        public string CodePostal { get; set; }
        public string Ville { get; set; }
        public string Region { get; set; }
        public string Pays { get; set; }
        public string Contact { get; set; }
        public string Telephone { get; set; }
        public string Portable { get; set; }

        public ClientLivraisonAdress(IBOFournisseur3 fournisseur3)
        {
            Intitule = fournisseur3.CT_Intitule ;
            Adresse = fournisseur3.Adresse.Adresse;
            Complement = fournisseur3.Adresse.Complement;
            CodePostal = fournisseur3.Adresse.CodePostal;
            Ville = fournisseur3.Adresse.Ville;
            Region = fournisseur3.Adresse.CodeRegion;
            Pays = fournisseur3.Adresse.Pays;
            if (String.IsNullOrEmpty(fournisseur3.CT_Contact))
            {
                Contact = fournisseur3.CT_Intitule; ;
            }
            else
            {
                Contact = fournisseur3.CT_Contact;
            }
            Telephone = fournisseur3.Telecom.Telephone;
            Portable = fournisseur3.Telecom.Portable;
        }

        public ClientLivraisonAdress()
        {

        }
    }
}
