using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using QQLike.Services;
using QQLike.ViewModels;

namespace QQLike.Views;

public partial class RegisterView : Window
{
    public RegisterView(RegisterViewModel registerViewModel)
    {
        InitializeComponent();
        this.SetViewModel(registerViewModel);
    }

    private void PasswordBox_GotFocus(object sender, RoutedEventArgs e)
    {
        PasswordRequirementsPanel.Visibility = Visibility.Visible;
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not PasswordBox passwordBox)
        {
            return;
        }

        var password = passwordBox.Password ?? string.Empty;

        if (DataContext is RegisterViewModel vm)
        {
            vm.Password = password;
        }

        var noSpace = password.All(ch => !char.IsWhiteSpace(ch));
        var validLength = password.Length is >= 8 and <= 16;
        var categoryCount = 0;
        if (password.Any(char.IsLetter)) categoryCount++;
        if (password.Any(char.IsDigit)) categoryCount++;
        if (password.Any(ch => !char.IsLetterOrDigit(ch) && !char.IsWhiteSpace(ch))) categoryCount++;
        var enoughCategory = categoryCount >= 2;
        var noLongPattern = !ContainsLongSequentialOrRepeated(password, 6);

        UpdateRuleIcon(NoSpaceIcon, noSpace);
        UpdateRuleIcon(LengthIcon, validLength);
        UpdateRuleIcon(CategoryIcon, enoughCategory);
        UpdateRuleIcon(SequenceIcon, noLongPattern);
    }

    private void PasswordBox_LostFocus(object sender, RoutedEventArgs e)
    {
        PasswordRequirementsPanel.Visibility = Visibility.Collapsed;
    }

    private static void UpdateRuleIcon(PackIcon icon, bool passed)
    {
        icon.Kind = passed ? PackIconKind.CheckCircleOutline : PackIconKind.CloseCircleOutline;
        icon.Foreground = passed
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00C853"))
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336"));
    }

    private static bool ContainsLongSequentialOrRepeated(string password, int minLength)
    {
        if (string.IsNullOrEmpty(password) || password.Length < minLength)
        {
            return false;
        }

        var repeated = 1;
        var asc = 1;
        var desc = 1;

        for (var i = 1; i < password.Length; i++)
        {
            var previous = password[i - 1];
            var current = password[i];

            if (char.IsLetterOrDigit(current) && current == previous)
            {
                repeated++;
                if (repeated >= minLength)
                {
                    return true;
                }
            }
            else
            {
                repeated = 1;
            }

            if (IsStep(previous, current, 1))
            {
                asc++;
            }
            else
            {
                asc = 1;
            }

            if (IsStep(previous, current, -1))
            {
                desc++;
            }
            else
            {
                desc = 1;
            }

            if (asc >= minLength || desc >= minLength)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsStep(char previous, char current, int step)
    {
        if (char.IsDigit(previous) && char.IsDigit(current))
        {
            return current - previous == step;
        }

        if (char.IsLetter(previous) && char.IsLetter(current))
        {
            return char.ToLowerInvariant(current) - char.ToLowerInvariant(previous) == step;
        }

        return false;
    }
}