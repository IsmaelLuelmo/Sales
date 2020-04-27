namespace Sales.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using GalaSoft.MvvmLight.Command;
    using Helpers;
    using Plugin.Media;
    using Plugin.Media.Abstractions;
    using Sales.Common.Models;
    using Services;
    using Xamarin.Forms;

    public class AddProductViewModel : BaseViewModel
    {
        #region Attributes
        private bool isRunning;
        private bool isEnabled;
        private ImageSource imageSource;
        private MediaFile file;
        #endregion

        #region Properties
        public ApiService ApiService { get; set; }

        public string Description { get; set; }

        public string Price { get; set; }

        public string Remarks { get; set; }

        public bool IsRunning
        {
            get { return this.isRunning; }
            set { this.SetValue(ref this.isRunning, value); }
        }

        public bool IsEnabled
        {
            get { return this.isEnabled; }
            set { this.SetValue(ref this.isEnabled, value); }
        }

        public ImageSource ImageSource
        {
            get { return this.imageSource; }
            set { this.SetValue(ref this.imageSource, value); }
        }
        #endregion

        #region Constructors
        public AddProductViewModel()
        {
            this.ApiService = new ApiService();
            this.IsEnabled = true;
            this.ImageSource = "noproduct";
        }
        #endregion

        #region Commands
        public ICommand SaveCommand 
        {
            get
            {
                return new RelayCommand(Save);
            }
        }

        public ICommand ChangeImageCommand
        {
            get
            {
                return new RelayCommand(ChangeImage);
            }
        }
        #endregion

        #region Methods
        //Método para grabar los datos en el WebAPI, previa validación
        private async void Save()
        {
            if (string.IsNullOrEmpty(this.Description))
            {
                _ = Application.Current.MainPage.DisplayAlert(Languages.Error,
                    Languages.DescriptionError,
                    Languages.Accept);
                return;
            }

            if (string.IsNullOrEmpty(this.Price))
            {
                _ = Application.Current.MainPage.DisplayAlert(Languages.Error,
                    Languages.PriceError,
                    Languages.Accept);
                return;
            }

            decimal price = Convert.ToDecimal(this.Price);
            if (price < 0)
            {
                _ = Application.Current.MainPage.DisplayAlert(Languages.Error,
                    Languages.PriceError,
                    Languages.Accept);
                return;
            }

            this.IsRunning = true;
            this.IsEnabled = false;


            var connection = await this.ApiService.CheckConnection();
            if (!connection.IsSuccess)
            {
                this.IsRunning = false;
                this.IsEnabled = true;
                await Application.Current.MainPage.DisplayAlert(Languages.Error,
                    connection.Message,
                    Languages.Accept);
                return;
            }

            var product = new Product
            {
                Description = this.Description,
                Price = price,
                Remarks = this.Remarks,
            };

            string url = Application.Current.Resources["UrlAPI"].ToString();
            string prefix = Application.Current.Resources["UrlPrefix"].ToString();
            string productsController = Application.Current.Resources["UrlProductsController"].ToString();
            var response = await this.ApiService.Post(url, prefix, productsController, product);

            if(!response.IsSuccess)
            {
                this.IsRunning = false;
                this.IsEnabled = true;
                _ = Application.Current.MainPage.DisplayAlert(Languages.Error,
                    response.Message,
                    Languages.Accept);
                return;
            }
            
            var newProduct = (Product)response.Result;
            var viewModel = ProductsViewModel.GetInstance();
            viewModel.Products.Add(newProduct);
            var list = viewModel.Products.OrderBy(p => p.Description).ToList();
            viewModel.Products=new ObservableCollection<Product>(list);            

            this.IsRunning = false;
            this.IsEnabled = true;

            await Application.Current.MainPage.Navigation.PopAsync();

            /* ALTERNATIVA PARA REFRESCAR
            ICommand comando;
            comando = ProductsViewModel.GetInstance().RefreshCommand;
            comando.Execute(null);*/

        }

        private async void ChangeImage()
        {
            await CrossMedia.Current.Initialize();

            var source = await Application.Current.MainPage.DisplayActionSheet(
                Languages.ImageSource,
                Languages.Cancel,
                null,
                Languages.FromGallery,
                Languages.NewPicture);

            if (source == Languages.Cancel)
            {
                this.file = null;
                return;
            }


                           
            if (source == Languages.NewPicture)
            {
                this.file = await CrossMedia.Current.TakePhotoAsync(
                    new StoreCameraMediaOptions
                    {
                        Directory = "Sample",
                        Name = "test.jpg",
                        PhotoSize = PhotoSize.Small,
                    }
                );
            }
            else
            {
                this.file = await CrossMedia.Current.PickPhotoAsync();
            }

            if (this.file != null)
            {
                this.ImageSource = ImageSource.FromStream(() =>
                {
                    var stream = this.file.GetStream();
                    return stream;
                });
            }
        }

        #endregion
    }
}
