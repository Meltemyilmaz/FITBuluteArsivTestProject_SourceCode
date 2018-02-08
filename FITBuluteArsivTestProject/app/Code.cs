using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Security.Cryptography;
using Ionic.Zip;
using UblInvoiceObject;
using FITBuluteArsivTestProject.ServiceRef;

namespace FITBuluteArsivTestProject.app
{
    public class Code
    {
        /// <summary>
        /// Basic Login
        /// </summary>
        /// <returns>Basic Login</returns>
        private string GetAuthorization(string username, string pass)
        {
            string authorization = username + ":" + pass; //kullanıcı adı ve şifre. aralarında : karakteri olması gerekiyor.
            byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(authorization);
            string base64authorization = Convert.ToBase64String(byteArray);

            return string.Format("Basic {0}", base64authorization);
        }

        /// <summary>
        /// Oluşturulan UBL faturayı xml'e çeviriyor. Hazır metod kopyalanabilir.
        /// </summary>
        /// <returns>UBL Fatura XML Dönüştürme</returns>
        private static string GetXML<T>(T obj)
        {
            XmlSerializer SerializerObj = new XmlSerializer(typeof(T));

            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            ns.Add("", "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2");
            ns.Add("cac", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2");
            ns.Add("cbc", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2");
            ns.Add("ccts", "urn:un:unece:uncefact:documentation:2");
            ns.Add("ds", "http://www.w3.org/2000/09/xmldsig#");
            ns.Add("ext", "urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2");
            ns.Add("qdt", "urn:oasis:names:specification:ubl:schema:xsd:QualifiedDatatypes-2");
            ns.Add("ubltr", "urn:oasis:names:specification:ubl:schema:xsd:TurkishCustomizationExtensionComponents");
            ns.Add("udt", "urn:un:unece:uncefact:data:specification:UnqualifiedDataTypesSchemaModule:2");
            ns.Add("udt", "urn:un:unece:uncefact:data:specification:UnqualifiedDataTypesSchemaModule:2");
            ns.Add("xades", "http://uri.etsi.org/01903/v1.3.2#");
            ns.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");

            MemoryStream ms = new MemoryStream();
            TextWriter WriteFileStream = new StreamWriter(ms, Encoding.UTF8);

            SerializerObj.Serialize(WriteFileStream, obj, ns);


            WriteFileStream.Close();

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        /// <summary>
        /// Dönüştürülen XML verisini zipleme işlemi yapıyor. .Net Freamwork 4.5 öncesi sistemlerde çalışmaz.
        /// </summary>
        /// <returns>XML Fatura yı Zipler .Net 4.5</returns>
        private byte[] ZipFile(string xml, string fileName)
        {
            //byte[] ziplenecekData = Encoding.UTF8.GetBytes(xml);

            //MemoryStream zipStream = new MemoryStream();

            //using (ZipArchive zip = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
            //{

            //    ZipArchiveEntry zipElaman = zip.CreateEntry(fileName + ".xml");
            //    Stream entryStream = zipElaman.Open();
            //    entryStream.Write(ziplenecekData, 0, ziplenecekData.Length);
            //    entryStream.Flush();
            //    entryStream.Close();

            //}

            //zipStream.Position = 0;
            //return zipStream.ToArray();

            return null;
        }

        /// <summary>
        /// Dönüştürülen XML verisini zipleme işlemi yapıyor. .Net Freamwork 4.5 öncesi sistemlerde için kullanılır. 3. parti uygulamadır. DDL proje içerisinde mevcut.
        /// </summary>
        /// <returns>XML Fatura yı Zipler .Net 3.5 sonrası</returns>
        private byte[] IonicZipFile(string xml, string fileName)
        {

            byte[] ziplenecekData = Encoding.UTF8.GetBytes(xml);

            MemoryStream zipStream = new MemoryStream();

            using (ZipFile zip = new Ionic.Zip.ZipFile())
            {
                ZipEntry zipEleman = zip.AddEntry(fileName + ".xml", ziplenecekData);

                zip.Save(zipStream);
            }

            zipStream.Seek(0, SeekOrigin.Begin);
            zipStream.Flush();

            zipStream.Position = 0;
            return zipStream.ToArray();
        }

        /// <summary>
        /// Gönderime hazır olan zip faturanın MD5 hash bilgisini döndürür.
        /// </summary>
        /// <returns>MD5 File Hash</returns>
        private string GetHashInfo(byte[] file)
        {
            using (var md5 = MD5.Create())
            {
                byte[] aa = md5.ComputeHash(file);

                var hash = BitConverter.ToString(aa).Replace("-", "").ToLower();

                return hash;
            }
        }

        /// <summary>
        /// UBL Olusturma Metodu
        /// </summary>
        /// <returns>UBL Fatura Tipi</returns>
        private InvoiceType CreateUBL(string TCKN_VKN)
        {
            Type aa = Type.GetType("System.Xml.XmlElement");

            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<xml />");

            InvoiceType earsiv = new InvoiceType()
            {
                UBLExtensions = new UBLExtensionType[]
                {
                    new UBLExtensionType()
                    {
                        ExtensionContent = doc.DocumentElement
                    }
                },

                UBLVersionID = new UBLVersionIDType { Value = "2.1" }, //uluslararası fatura standardı 2.1
                CustomizationID = new CustomizationIDType { Value = "TR1.2" }, //fakat GİB UBLTR olarak isimlendirdiği Türkiye'ye özgü 1.2 efatura formatını kullanıyor.
                ProfileID = new ProfileIDType { Value = "EARSIVFATURA" }, //GİB 28.04.2017 tarihli duruyurusundan sonra e-Arşiv faturaları için Profile ID sadece EARSIVFATURA olmak zorundadır.
                ID = new IDType { Value = "FIT2017032200132" },
                CopyIndicator = new CopyIndicatorType { Value = false }, //kopyası mı, asıl süret mi olduğu belirlenir
                UUID = new UUIDType { Value = Guid.NewGuid().ToString() }, //fatura uuid
                IssueDate = new IssueDateType { Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second) }, //fatura tarihi
                IssueTime = new IssueTimeType { Value = default(DateTime).AddHours(11).AddMinutes(20) },
                InvoiceTypeCode = new InvoiceTypeCodeType { Value = "SATIS" }, //gönderilecek fatura çeşidi, satış, iade vs.
                DocumentCurrencyCode = new DocumentCurrencyCodeType { Value = "TRY" }, //efatura para birimi
                LineCountNumeric = new LineCountNumericType { Value = 1 }, //fatura kalemlerinin sayısı

                Note = new NoteType[]
                {
                    new NoteType()
                    {
                        Value = "Fatura Notu"
                    }
                },

                #region AdditionalDocumentReference

                AdditionalDocumentReference = new DocumentReferenceType[]
                {
                    new DocumentReferenceType()
                    {
                        ID = new IDType { Value = Guid.NewGuid().ToString() },
                        IssueDate = new IssueDateType { Value = DateTime.Now },
                        DocumentTypeCode = new DocumentTypeCodeType { Value = "CUST_INV_ID" }
                    },

                    new DocumentReferenceType()
                    {
                        ID = new IDType { Value = "0100" },
                        IssueDate = new IssueDateType { Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second) },
                        DocumentTypeCode = new DocumentTypeCodeType { Value = "OUTPUT_TYPE" }
                    },
                    
                    new DocumentReferenceType() //efatura dan farklı olarak sadece bu alan eklenmiştir. 
                    {
                        ID = new IDType { Value = "KAGIT" },
                        IssueDate = new IssueDateType { Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second) },
                        DocumentTypeCode = new DocumentTypeCodeType { Value = "EREPSENDT" }  
                    },

                    new DocumentReferenceType()
                    {
                        ID = new IDType { Value = "99" },
                        IssueDate = new IssueDateType { Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second) },
                        DocumentTypeCode = new DocumentTypeCodeType { Value = "TRANSPORT_TYPE" }
                    }
                },

                #endregion

                #region //Signature

                Signature = new SignatureType[]
                {
                    new SignatureType
                    {
                        ID = new IDType { schemeID = "VKN_TCKN", Value = "1212121212" },
                        SignatoryParty = new PartyType
                        {
                            PartyIdentification = new PartyIdentificationType[]
                            {
                                new PartyIdentificationType()
                                {
                                    ID = new IDType { schemeID = "VKN", Value = "1212121212" }
                                }
                            },

                            PostalAddress = new AddressType
                            {
                                StreetName = new StreetNameType { Value = "Deneme Cadde, Deneme Sokak." },
                                BuildingName = new BuildingNameType { Value = "01" },
                                CitySubdivisionName = new CitySubdivisionNameType { Value = "İlçe" },
                                CityName = new CityNameType { Value = "İL" },
                                PostalZone = new PostalZoneType { Value = "34000" },
                                Country = new CountryType { Name = new NameType1 { Value = "TÜRKİYE" } }
                            }
                        },

                        DigitalSignatureAttachment = new AttachmentType
                        {
                            ExternalReference = new ExternalReferenceType
                            {
                                URI = new URIType { Value = "#Signature" }
                            }
                        }
                    },
                },

                #endregion

                #region AccountingSupplierParty

                AccountingSupplierParty = new SupplierPartyType //gönderenin fatura üzerindeki bilgileri
                {
                    Party = new PartyType()
                    {
                        WebsiteURI = new WebsiteURIType { Value = "web sitesi" },

                        PartyIdentification = new PartyIdentificationType[]
                        {
                            new PartyIdentificationType() { ID = new IDType { schemeID = "VKN", Value = TCKN_VKN } }
                        },

                        PartyName = new PartyNameType { Name = new NameType1 { Value = "AAA Anonim Şirketi" } },

                        PostalAddress = new AddressType
                        {
                            ID = new IDType { Value = "1234567890" },
                            BuildingNumber = new BuildingNumberType { Value = "bina no" },
                            StreetName = new StreetNameType { Value = "cadde" },
                            BuildingName = new BuildingNameType { Value = "bina" },
                            CitySubdivisionName = new CitySubdivisionNameType { Value = "mahalle" },
                            CityName = new CityNameType { Value = "sehir" },
                            PostalZone = new PostalZoneType { Value = "posta kodu" },
                            Country = new CountryType { Name = new NameType1 { Value = "ülke" } }
                        },

                        PartyTaxScheme = new PartyTaxSchemeType
                        {
                            TaxScheme = new TaxSchemeType { Name = new NameType1 { Value = "vergi dairesi" } }
                        },

                        Contact = new ContactType
                        {
                            Telephone = new TelephoneType { Value = "telefon" },
                            Telefax = new TelefaxType { Value = "faks" },
                            ElectronicMail = new ElectronicMailType { Value = "mail" }
                        }
                    }
                },

                #endregion

                #region AccountingCustomerParty

                AccountingCustomerParty = new CustomerPartyType //Alıcının fatura üzerindeki bilgileri
                {
                    Party = new PartyType
                    {
                        WebsiteURI = new WebsiteURIType { Value = "http:\\www.gib.gov.tr" },
                        PartyName = new PartyNameType { Name = new NameType1 { Value = "ALICI ŞİRKET" } }, //ünvan

                        PartyIdentification = new PartyIdentificationType[]
                        {
                            new PartyIdentificationType()
                            {
                               
                                ID = new IDType { schemeID = "VKN", Value = "0000000000", }
                            },

                            new PartyIdentificationType()
                            {
                                ID = new IDType { schemeID = "TESISATNO", Value = TCKN_VKN }
                            },

                            new PartyIdentificationType()
                            {
                                ID = new IDType { schemeID = "SAYACNO", Value = TCKN_VKN }
                            }
                        },

                        PostalAddress = new AddressType
                        {
                            ID = new IDType { Value = "EV ADRESİ" },
                            StreetName = new StreetNameType { Value = "Etlik Cad." },
                            BuildingName = new BuildingNameType { Value = "Gelir İdaresi Ek Hizmet Binası" },
                            BuildingNumber = new BuildingNumberType { Value = "16" },
                            CitySubdivisionName = new CitySubdivisionNameType { Value = "Dışkapı" },
                            CityName = new CityNameType { Value = "Ankara" },
                            PostalZone = new PostalZoneType { Value = "06110" },

                            Country = new CountryType { Name = new NameType1 { Value = "Türkiye" } }
                        },

                        Contact = new ContactType
                        {
                            Telephone = new TelephoneType { Value = "asdasd" },
                            Telefax = new TelefaxType { Value = "asdasd" },
                            ElectronicMail = new ElectronicMailType { Value = "efatura@gib.gov.tr" }
                        },

                        Person = new PersonType
                        {
                            FirstName = new FirstNameType { Value = "İsim" },
                            FamilyName = new FamilyNameType { Value = "Soyisim" }
                        }
                    }
                },

                #endregion

                #region Delivery

                Delivery = new DeliveryType[]
                {
                    new DeliveryType()
                    {
                        DeliveryAddress = new AddressType() //Teslimat Adresi
                        {
                            StreetName = new StreetNameType { Value = "Talatpaşa Cad." },
                            CitySubdivisionName = new CitySubdivisionNameType { Value = "Ümraniye" },
                            CityName = new CityNameType { Value = "İstanbul" },
                            Country = new CountryType { Name = new NameType1 { Value = "Türkiye" } }
                        },

                        CarrierParty = new PartyType //Teslimatı gerçekleştiren firma (kargo vs.)
                        {
                            PartyIdentification = new PartyIdentificationType[]
                            {
                                new PartyIdentificationType()
                                {
                                    ID = new IDType { schemeID = "VKN", Value = "1234567890" }
                                }
                            },

                            PartyName = new PartyNameType { Name = new NameType1 { Value = "Aras Kargo" } },

                            PostalAddress = new AddressType
                            {
                                ID = new IDType { Value = "" },
                                StreetName = new StreetNameType { Value = "Rüzgarlıbahçe Mah. Yavuz Sultan Selim Cad." },
                                BuildingName = new BuildingNameType { Value = "Aras Plaza" },
                                BuildingNumber = new BuildingNumberType { Value = "2" },
                                CitySubdivisionName = new CitySubdivisionNameType { Value = "Beykoz" },
                                CityName = new CityNameType { Value = "İstanbul" },
                                PostalZone = new PostalZoneType { Value = "34000" },
                                Country = new CountryType { Name = new NameType1 { Value = "Türkiye" } }
                            }
                        },

                        DeliveryParty = new PartyType //Teslimat Bilgileri
                        {
                            PartyIdentification = new PartyIdentificationType[]
                            {
                                new PartyIdentificationType()
                                {
                                    ID = new IDType { Value = "" }
                                }
                            },

                            PartyName = new PartyNameType { Name = new NameType1 { Value = "Teslimat yapılacak isim"} },

                            PostalAddress = new AddressType
                            {
                                ID = new IDType { Value = "" },
                                StreetName = new StreetNameType { Value = "Talatpaşa Cad. Park Sok." },
                                BuildingNumber = new BuildingNumberType { Value = "35-1" },
                                CitySubdivisionName = new CitySubdivisionNameType { Value = "Ümraniye" },
                                CityName = new CityNameType { Value = "İstanbul" },
                                PostalZone = new PostalZoneType { Value = "34000" },
                                Country = new CountryType { Name = new NameType1 { Value = "Türkiye" } }
                            },

                            Person = new PersonType
                            {
                                FirstName = new FirstNameType { Value = "Teslim alacak kişi isim" },
                                FamilyName = new FamilyNameType { Value = "Teslim alacak kişi soyisim" }
                            }
                        }
                    }
                },

                #endregion

                #region PaymentTerms

                PaymentTerms = new PaymentTermsType
                {
                    Note = new NoteType { Value = "BBB Bank Otomatik Ödeme" }
                },

                TaxTotal = new TaxTotalType[]
                {
                    new TaxTotalType()
                    {
                        TaxAmount = new TaxAmountType { currencyID = "TRY", Value = 2.73M },

                        TaxSubtotal = new TaxSubtotalType[]
                        {
                            new TaxSubtotalType()
                            {
                                TaxableAmount = new TaxableAmountType { currencyID = "TRY", Value = 15.15M },
                                TaxAmount = new TaxAmountType { currencyID = "TRY", Value = 2.73M },
                                TaxCategory = new TaxCategoryType { TaxScheme = new TaxSchemeType { TaxTypeCode = new TaxTypeCodeType { Value = "0015" } } }
                            },
                        }
                    }
                },

                #endregion

                #region LegalMonetaryTotal

                LegalMonetaryTotal = new MonetaryTotalType
                {
                    LineExtensionAmount = new LineExtensionAmountType { currencyID = "TRY", Value = 15.15M },
                    TaxExclusiveAmount = new TaxExclusiveAmountType { currencyID = "TRY", Value = 15.15M },
                    TaxInclusiveAmount = new TaxInclusiveAmountType { currencyID = "TRY", Value = 17.88M },
                    PayableAmount = new PayableAmountType { currencyID = "TRY", Value = 17.88M }
                },

                #endregion

                #region InvoiceLine

                InvoiceLine = SetInvoiceLine()

                #endregion
            };

            return earsiv;
        }

        /// <summary>
        /// Fatura kalemlerini dolduran metod
        /// </summary>
        /// <returns></returns>
        private InvoiceLineType[] SetInvoiceLine()
        {
            List<InvoiceLineType> faturaKalemList = new List<InvoiceLineType>();

            for (int i = 0; i < 10; i++)
            {
                InvoiceLineType faturaKalem = new InvoiceLineType();

                faturaKalem.ID = new IDType() { Value = i.ToString() };
                faturaKalem.InvoicedQuantity = new InvoicedQuantityType { unitCode = "KWH", Value = 101 };
                faturaKalem.LineExtensionAmount = new LineExtensionAmountType { currencyID = "TRY", Value = 15.15M };

                faturaKalem.AllowanceCharge = new AllowanceChargeType[]
                {
                    new AllowanceChargeType()
                    {
                        ChargeIndicator = new ChargeIndicatorType { Value = false },
                        MultiplierFactorNumeric = new MultiplierFactorNumericType { Value = 0.00M },
                        Amount = new AmountType2 { currencyID = "TRY", Value = 0.01M },
                        BaseAmount = new BaseAmountType { currencyID = "TRY", Value = 15.15M }
                    }
                };

                faturaKalem.TaxTotal = new TaxTotalType
                {
                    TaxAmount = new TaxAmountType { currencyID = "TRY", Value = 2.73M },

                    TaxSubtotal = new TaxSubtotalType[]
                    {
                        new TaxSubtotalType()
                        {
                            TaxableAmount = new TaxableAmountType { currencyID = "TRY", Value = 15.15M },
                            TaxAmount = new TaxAmountType { currencyID = "TRY", Value = 2.73M },
                            Percent = new PercentType1 { Value = 18.0M }, 

                            TaxCategory = new TaxCategoryType()
                            {
                                TaxScheme = new TaxSchemeType 
                                { 
                                    Name = new NameType1 { Value = "KDV" }, 
                                    TaxTypeCode = new TaxTypeCodeType { Value = "0015" }
                                }
                            }
                        }
                    }
                };

                faturaKalem.Item = new ItemType
                {
                    Name = new NameType1 { Value = "Elektrik Tüketim Bedeli" }
                };

                faturaKalem.Price = new PriceType
                {
                    PriceAmount = new PriceAmountType { currencyID = "TRY", Value = 0.15M }
                };


                faturaKalemList.Add(faturaKalem);
            }

            return faturaKalemList.ToArray();
        }

        /// <summary>
        /// Sisteme e-Arşiv faturası gönderme
        /// </summary>
        /// <returns>Sisteme e-Arşiv faturası gönderme</returns>
        public sendInvoiceResponseType FaturaGonder(string TCKN_VKN, string sube, string wsUserName, string wsPass)
        {
            //proje içerisine include edilen UBL-Invoice-2_1.cs dosyasına namespace ekleyerek projemize dahil ediyoruz.
            InvoiceType createdUbl = CreateUBL(TCKN_VKN); //bu metod ile göndereceğimiz e-arşiv faturasının parametrelerini set ediyoruz.

            string strFatura = GetXML(createdUbl); //CreateUBL metodundan dönen veriyi xml'e çeviriyoruz. hazır metod sizde kopyalayabilirsiniz.

            strFatura = strFatura.Replace("<xml xmlns=\"\" />", ""); //create ubl metodunda invoice tagının hemen altında oluşturduğumuz extension alanına XmlDocument olarak set edilen veriyi siliyoruz.

            byte[] byteFatura = System.Text.Encoding.ASCII.GetBytes(strFatura); //xml verisini byte tipine çeviriyoruz.

            //burda dikkat edilmesi gereken kısım, ZipFile() metodu (projenin üst kısmında) sadece .net 4.5 da çalışmaktadır. Öncesi sistemler için 3rd party ionic zip kullanılmaktadır. DLL dosyası projenin içinde mevcut.
            byte[] zipliFile = IonicZipFile(strFatura, createdUbl.UUID.Value); //xml olarak dönüştürülen e-arşiv faturasını zip dosyası içine ekliyoruz.

            //diğer bir nokta, xml olarak gelen veriyi ve zip dosyasını fiziksel dosya olarak herhangi bir yere kayıt etmiyoruz. bellekte saklanan veriyi gönderiyoruz.


            string hash = GetHashInfo(zipliFile); //ziplenen e-arşiv faturasının hash bilgisini alıyoruz.


            eArsivInvoicePortTypeClient wsClient = new eArsivInvoicePortTypeClient();

            using (new System.ServiceModel.OperationContextScope((System.ServiceModel.IClientChannel)wsClient.InnerChannel))
            {

                System.ServiceModel.Web.WebOperationContext.Current.OutgoingRequest.Headers.Add(HttpRequestHeader.Authorization, GetAuthorization(wsUserName, wsPass));

                var req = new sendInvoiceRequest() //fatura göndermek için request parametrelerini set ediyoruz.
                {
                    sendInvoiceRequestType = new SendInvoiceRequestType
                    {
                        senderID = TCKN_VKN, //gönderici VKN-TCKN
                        receiverID = "1111111111", //alıcı VKN-TCKN
                        fileName = createdUbl.UUID.Value, //dosya ismi
                        binaryData = zipliFile, //gönderilecek fatura
                        docType = "XML", //dosya tipi 
                        hash = hash, //dosyanın hash bilgisi

                        customizationParams = new CustomizationParam[]
                        {
                            new CustomizationParam()
                            {
                                paramName = "BRANCH", //parametre ismi
                                paramValue = sube //şube adı. opsiyoneldir. gönderilmez ise varsayılan olarak "default" şube setlenir.
                            }
                        },

                        responsiveOutput = new ResponsiveOutput //gönerilen faturanın dönen cevabında binary olarak fatura görüntüsü almak için. opsiyonel alan.
                        {
                            outputType = ResponsiveOutputType.PDF,
                            outputTypeSpecified = true
                        }
                    }
                };

                var result = wsClient.sendInvoice(req.sendInvoiceRequestType);

                return result;
            }
        }

        /// <summary>
        /// Sisteme gönderilen e-arşiv faturasının durumunu sorgulama
        /// </summary>
        /// <returns>Sisteme gönderilen e-arşiv faturasının durumunu sorgulama</returns>
        public getStatusResponseType FaturaDurumSorgula(string TCKN_VKN, string uuid, string invid, string wsUserName, string wsPass)
        {
            eArsivInvoicePortTypeClient wsClient = new eArsivInvoicePortTypeClient();

            using (new System.ServiceModel.OperationContextScope((System.ServiceModel.IClientChannel)wsClient.InnerChannel))
            {

                System.ServiceModel.Web.WebOperationContext.Current.OutgoingRequest.Headers.Add(HttpRequestHeader.Authorization, GetAuthorization(wsUserName, wsPass));

                var req = new getStatusRequest() //request parametrelerini set ediyoruz.
                {
                    getStatusRequestType = new getStatusRequestType()
                    {
                        vkn = TCKN_VKN,
                        UUID = uuid,
                        invoiceNumber = invid
                    }
                };

                var result = wsClient.getStatus(req.getStatusRequestType);

                return result;
            }
        }

        /// <summary>
        /// Textbox a girilen UUID ve Invoice ID alanlarını okuyarak faturanın görüntüsünü HTML olarak indirir.
        /// </summary>
        /// <returns>Textbox a girilen UUID ve Invoice ID alanlarını okuyarak faturanın görüntüsünü HTML olarak indirir.</returns>
        public getInvoiceDocumentResponseType FaturaIndir(string TCKN_VKN, string uuid, string invid, string wsUserName, string wsPass)
        {
            eArsivInvoicePortTypeClient wsClient = new eArsivInvoicePortTypeClient();

            using (new System.ServiceModel.OperationContextScope((System.ServiceModel.IClientChannel)wsClient.InnerChannel))
            {
                System.ServiceModel.Web.WebOperationContext.Current.OutgoingRequest.Headers.Add(HttpRequestHeader.Authorization, GetAuthorization(wsUserName, wsPass));

                var req = new getInvoiceDocumentRequest() //indirilmek istenen e-arşiv faturasının request parametrelerini set ediyoruz.
                {
                    getInvoiceDocumentRequestType = new getInvoiceDocumentRequestType
                    {
                        vkn = TCKN_VKN,
                        UUID = uuid,
                        invoiceNumber = invid,
                        outputType = "HTML",
                    }
                };

                var result = wsClient.getInvoiceDocument(req.getInvoiceDocumentRequestType);

                return result;
            }
        }
    }
}
