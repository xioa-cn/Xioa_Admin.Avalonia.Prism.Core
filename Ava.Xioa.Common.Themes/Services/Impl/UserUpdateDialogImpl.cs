using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Input;
using Ava.Xioa.Common.Models;
using Ava.Xioa.Common.Themes.Services.Services;
using Ava.Xioa.Entities.SystemDbset.SystemThemesInformation.Mapper;
using Avalonia.Collections;
using Microsoft.Extensions.DependencyInjection;
using SukiUI.Controls;
using SukiUI.Dialogs;

namespace Ava.Xioa.Common.Themes.Services.Impl;

public partial class ViewUserInformation: ReactiveObject
{
    #region NotityProperty

    private string _account = "";

    public string Account
    {
        get => _account;
        set => this.SetProperty(ref _account, value);
    }

    private string _password = "";

    public string Password
    {
        get => _password;
        set => this.SetProperty(ref _password, value);
    }

    private string _userName = "";

    public string UserName
    {
        get => _userName;
        set => this.SetProperty(ref _userName, value);
    }

    private UserAuthEnum _userAuth = UserAuthEnum.None;

    public UserAuthEnum UserAuth
    {
        get => _userAuth;
        set => this.SetProperty(ref _userAuth, value);
    }

    #endregion
}

[PrismService(typeof(IUserUpdateDialogServices), ServiceLifetime.Singleton)]
public class UserUpdateDialogImpl : ReactiveObject, IUserUpdateDialogServices
{
    public ViewUserInformation View { get; set; } = new ViewUserInformation();

    public IAvaloniaReadOnlyList<CategoryViewModel> Categories { get; }

    public ISukiDialog? SukiDialog { get; set; }
    public ICommand CancelCommand { get; }
    public ICommand OkCommand { get; }
    public UserInformation? UserInformation { get; set; }

    public Func<UserInformation?, Task<bool>>? OkFuncAsync { get; set; }
    public Action? OkError { get; set; }

    public void SetUserInformation(UserInformation userInformation)
    {
        UserInformation = userInformation;
        View.UserName = userInformation.UserName;
        View.Password = userInformation.Password;
        View.UserAuth = userInformation.UserAuth;
    }

    private static string? GetCategory(PropertyInfo property)
    {
        var attributes = property.GetCustomAttributes<CategoryAttribute>(false);
        if (attributes.Any())
        {
            return attributes.First().Category;
        }

        return "Properties";
    }

    private static string? GetDisplayName(PropertyInfo property)
    {
        var attributes = property.GetCustomAttributes<DisplayNameAttribute>(false);
        if (attributes.Any())
        {
            return attributes.First().DisplayName;
        }

        return null;
    }

    private IAvaloniaReadOnlyList<CategoryViewModel> GenerateCategories()
    {
        var properties = View.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToList();
        var categories = properties
            .Select(prop => (Property: prop, Category: GetCategory(prop), DisplayName: GetDisplayName(prop)))
            .Where(p => p.Category is not null)
            .Distinct()
            .GroupBy(p => p.Category);
        
        var categoryViewModels = new AvaloniaList<CategoryViewModel>();
        
        foreach (var grouping in categories)
        {
            var propertyViewModels = new AvaloniaList<IPropertyViewModel>();
            foreach (var (Property, Category, DisplayName) in grouping)
            {
                var propertyViewModel = default(IPropertyViewModel?);
                var displayname = DisplayName ?? Property.Name;

                if (Property.PropertyType == typeof(string))
                {
                    propertyViewModel = new StringViewModel(View, displayname, Property);
                }
                else if (Property.PropertyType == typeof(int) || Property.PropertyType == typeof(int?))
                {
                    propertyViewModel = new IntegerViewModel(View, displayname, Property);
                }
                else if (Property.PropertyType == typeof(long) || Property.PropertyType == typeof(long?))
                {
                    propertyViewModel = new LongViewModel(View, displayname, Property);
                }
                else if (Property.PropertyType == typeof(double) || Property.PropertyType == typeof(double?))
                {
                    propertyViewModel = new DoubleViewModel(View, displayname, Property);
                }
                else if (Property.PropertyType == typeof(float) || Property.PropertyType == typeof(float?))
                {
                    propertyViewModel = new FloatViewModel(View, displayname, Property);
                }
                else if (Property.PropertyType == typeof(decimal) || Property.PropertyType == typeof(decimal?))
                {
                    propertyViewModel = new DecimalViewModel(View, displayname, Property);
                }
                else if (Property.PropertyType == typeof(bool) || Property.PropertyType == typeof(bool?))
                {
                    propertyViewModel = new BoolViewModel(View, displayname, Property);
                }
                else if (Property.PropertyType.IsEnum)
                {
                    propertyViewModel = new EnumViewModel(View, displayname, Property);
                }
                else if (Property.PropertyType == typeof(DateTime) || Property.PropertyType == typeof(DateTime?))
                {
                    propertyViewModel = new DateTimeViewModel(View, displayname, Property);
                }
                else if (Property.PropertyType == typeof(DateTimeOffset) ||
                         Property.PropertyType == typeof(DateTimeOffset?))
                {
                    propertyViewModel = new DateTimeOffsetViewModel(View, displayname, Property);
                }
                else
                {
                    var propertyValue = Property.GetValue(this) as INotifyPropertyChanged;
                    if (propertyValue is INotifyPropertyChanged childViewModel)
                    {
                        propertyViewModel = new ComplexTypeViewModel(View, displayname, Property);
                    }
                }

                if (propertyViewModel is not null)
                {
                    propertyViewModels.Add(propertyViewModel);
                }
            }

            var categoryViewModel = new CategoryViewModel(grouping.Key!, propertyViewModels);
            categoryViewModels.Add(categoryViewModel);
        }

        return categoryViewModels;
    }

    public UserUpdateDialogImpl()
    {
        CancelCommand = new RelayCommand(Cancel);
        OkCommand = new RelayCommand(Ok);
        Categories = GenerateCategories();
    }

    private async void Ok()
    {
        if (this.UserInformation is null)
        {
            throw new NullReferenceException(nameof(UserInformation));
        }

        this.UserInformation.Account = View.Account;
        this.UserInformation.Password = View.Password;
        this.UserInformation.UserName = View.UserName;
        this.UserInformation.UserAuth = View.UserAuth;
        if (OkFuncAsync is null)
        {
            CloseDialog();
            return;
        }

        var result = await OkFuncAsync.Invoke(this.UserInformation);
        if (result)
        {
            CloseDialog();
        }
        else
        {
            OkError?.Invoke();
        }
    }

    public void CloseDialog()
    {
        SukiDialog?.Dismiss();
        SukiDialog?.ResetToDefault();
    }

    private void Cancel()
    {
        CloseDialog();
    }

    public UserUpdateDialogImpl(UserInformation userInformation) : this()
    {
        UserInformation = userInformation;
    }
}