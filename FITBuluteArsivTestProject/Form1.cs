using FITBuluteArsivTestProject.app;
using FITBuluteArsivTestProject.ServiceRef;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Windows.Forms;
using UblInvoiceObject;

namespace FITBuluteArsivTestProject
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Formun da bağlantı için kullanılan alanların dolu olmasını kontrol eder.
        /// </summary>
        /// <returns></returns>
        private bool CheckConnParam()
        {
            bool check = true;

            foreach (var item in panel1.Controls)
            {
                if (item.GetType() == typeof(TextBox))
                {
                    TextBox t = (TextBox)item;
                    if (string.IsNullOrEmpty(t.Text.Trim()))
                        check = false;
                }
            }

            return check;
        }

        private void btnFaturaGonder_Click(object sender, EventArgs e)
        {
            if (CheckConnParam())
            {
                try
                {
                    Code xCode = new Code();

                    var result = xCode.FaturaGonder(txtTcVkn.Text.Trim(), txtSube.Text.Trim(), txtKullanici.Text.Trim(), txtSifre.Text.Trim());


                    if (result.Result.Result1 == ResultType.SUCCESS)
                    {
                        MessageBox.Show(result.Detail, "Response", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        txtFaturaUUID.Text = result.preCheckSuccessResults[0].UUID;
                        txtFaturaID.Text = result.preCheckSuccessResults[0].InvoiceNumber;

                        if (result.preCheckSuccessResults[0].binaryData != null)
                        {
                            FolderBrowserDialog fbDialog = new FolderBrowserDialog();
                            fbDialog.Description = "Lütfen kaydetmek istediğiniz dizini seçiniz...";
                            fbDialog.RootFolder = Environment.SpecialFolder.Desktop;

                            if (fbDialog.ShowDialog() == DialogResult.OK)
                            {
                                //dialog ile kullanıcıya seçtirilen dizine fatura UUID si ile dosya ismini set ederek kayıt işlemi yapıyoruz.
                                File.WriteAllBytes(fbDialog.SelectedPath + "\\" + result.preCheckSuccessResults[0].Filename + ".pdf", result.preCheckSuccessResults[0].binaryData);

                                MessageBox.Show("Fatura PDF olarak kaydedildi.", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                        else
                        {
                            MessageBox.Show("Fatura görüntüsü kaydedilemiyor. Lütfen XSLT ayarlarınızın yapıldığından emin olunuz...", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show(result.preCheckErrorResults[0].ErrorDesc, "Response", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (FaultException<processingFaultType> ex)
                {
                    MessageBox.Show(ex.Detail.Text, "ProcessingFault", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (FaultException ex)
                {
                    MessageBox.Show(ex.Message, "FaultException", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
            else
            {
                MessageBox.Show("TCKN/VKN, Şube, WS Kullanıcı Adı ve WS Şifre alanları boş bırakılamaz!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnFaturaDurumSorgula_Click(object sender, EventArgs e)
        {
            if (CheckConnParam())
            {
                try
                {
                    Code xCode = new Code();

                    var result = xCode.FaturaDurumSorgula(txtTcVkn.Text.Trim(), txtFaturaUUID.Text, txtFaturaID.Text, txtKullanici.Text.Trim(), txtSifre.Text.Trim());

                    MessageBox.Show(result.Detail, "", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (FaultException<processingFaultType> ex)
                {
                    MessageBox.Show(ex.Detail.Text, "ProcessingFault", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (FaultException ex)
                {
                    MessageBox.Show(ex.Message, "FaultException", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
            else
            {
                MessageBox.Show("TCKN/VKN, Şube, WS Kullanıcı Adı ve WS Şifre alanları boş bırakılamaz!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnFaturaIndir_Click(object sender, EventArgs e)
        {
            if (CheckConnParam())
            {
                try
                {
                    Code xCode = new Code();

                    var result = xCode.FaturaIndir(txtTcVkn.Text.Trim(), txtFaturaUUID.Text, txtFaturaID.Text, txtKullanici.Text.Trim(), txtSifre.Text.Trim());

                    FolderBrowserDialog fbDialog = new FolderBrowserDialog();
                    fbDialog.Description = "Lütfen kaydetmek istediğiniz dizini seçiniz...";
                    fbDialog.RootFolder = Environment.SpecialFolder.Desktop;

                    if (fbDialog.ShowDialog() == DialogResult.OK)
                    {
                        //dialog ile kullanıcıya seçtirilen dizine fatura UUID si ile dosya ismini set ederek kayıt işlemi yapıyoruz.
                        File.WriteAllBytes(fbDialog.SelectedPath + "\\" + txtFaturaUUID.Text + ".html", result.binaryData);

                        MessageBox.Show("Fatura indirme başarılı", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (FaultException<processingFaultType> ex)
                {
                    MessageBox.Show(ex.Detail.Text, "ProcessingFault", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (FaultException ex)
                {
                    MessageBox.Show(ex.Message, "FaultException", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
            else
            {
                MessageBox.Show("TCKN/VKN, Şube, WS Kullanıcı Adı ve WS Şifre alanları boş bırakılamaz!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
