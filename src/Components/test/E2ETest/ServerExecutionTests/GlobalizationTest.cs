// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests
{
    // For now this is limited to server-side execution because we don't have the ability to set the
    // culture in client-side Blazor.
    public class GlobalizationTest : BasicTestAppTestBase
    {
        public GlobalizationTest(
            BrowserFixture browserFixture,
            ToggleExecutionModeServerFixture<Program> serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture.WithServerExecution(), output)
        {
        }

        protected override void InitializeAsyncCore()
        {
            // On WebAssembly, page reloads are expensive so skip if possible
            Navigate(ServerPathBase, _serverFixture.ExecutionMode == ExecutionMode.Client);
            MountTestComponent<CulturePicker>();
            WaitUntilExists(By.Id("culture-selector"));
        }

        [Theory]
        [InlineData("en-US")]
        [InlineData("fr-FR")]
        public void CanSetCultureAndParseCultueSensitiveNumbersAndDates(string culture)
        {
            var cultureInfo = CultureInfo.GetCultureInfo(culture);

            var selector = new SelectElement(Browser.FindElement(By.Id("culture-selector")));
            selector.SelectByValue(culture);

            // That should have triggered a redirect, wait for the main test selector to come up.
            MountTestComponent<GlobalizationBindCases>();
            WaitUntilExists(By.Id("globalization-cases"));

            var cultureDisplay = WaitUntilExists(By.Id("culture-name-display"));
            Assert.Equal($"Culture is: {culture}", cultureDisplay.Text);

            // int
            var input = Browser.FindElement(By.Id("input_type_text_int"));
            var display = Browser.FindElement(By.Id("input_type_text_int_value"));
            Browser.Equal(42.ToString(cultureInfo), () => display.Text);

            input.Clear();
            input.SendKeys(9000.ToString("0,000", cultureInfo));
            input.SendKeys("\t");
            Browser.Equal(9000.ToString(cultureInfo), () => display.Text);

            // decimal
            input = Browser.FindElement(By.Id("input_type_text_decimal"));
            display = Browser.FindElement(By.Id("input_type_text_decimal_value"));
            Browser.Equal(4.2m.ToString(cultureInfo), () => display.Text);

            input.Clear();
            input.SendKeys(9000.42m.ToString("0,000.00", cultureInfo));
            input.SendKeys("\t");
            Browser.Equal(9000.42m.ToString(cultureInfo), () => display.Text);

            // datetime
            input = Browser.FindElement(By.Id("input_type_text_datetime"));
            display = Browser.FindElement(By.Id("input_type_text_datetime_value"));
            Browser.Equal(new DateTime(1985, 3, 4).ToString(cultureInfo), () => display.Text);

            ReplaceText(input, new DateTime(2000, 1, 2).ToString(cultureInfo));
            input.SendKeys("\t");
            Browser.Equal(new DateTime(2000, 1, 2).ToString(cultureInfo), () => display.Text);

            // datetimeoffset
            input = Browser.FindElement(By.Id("input_type_text_datetimeoffset"));
            display = Browser.FindElement(By.Id("input_type_text_datetimeoffset_value"));
            Browser.Equal(new DateTimeOffset(new DateTime(1985, 3, 4)).ToString(cultureInfo), () => display.Text);

            ReplaceText(input, new DateTimeOffset(new DateTime(2000, 1, 2)).ToString(cultureInfo));
            input.SendKeys("\t");
            Browser.Equal(new DateTimeOffset(new DateTime(2000, 1, 2)).ToString(cultureInfo), () => display.Text);
        }

        // The logic is different for verifying culture-invariant fields. The problem is that the logic for what
        // kinds of text a field accepts is determined by the browser and language - it's not general. So while
        // type="number" and type="date" produce fixed-format and culture-invariant input/output via the "value"
        // attribute - the actual input processing is harder to nail down. In practice this is only a problem
        // with dates.
        // 
        // For this reason we avoid sending keys directly to the field, and let two-way binding do its thing instead.
        //
        // A brief summary:
        // 1. Input a value (invariant culture if using number field, or current culture to extra input if using date field)
        // 2. trigger onchange
        // 3. Verify "value" field (current culture)
        // 4. Verify the input field's value attribute (invariant culture)
        //
        // We need to do step 4 to make sure that the value we're entering can "stick" in the form field.
        // We can't use ".Text" because DOM reasons :(
        [Theory]
        [InlineData("en-US")]
        [InlineData("fr-FR")]
        public void CanSetCultureAndParseCultureInvariantNumbersAndDatesWithInputFields(string culture)
        {
            var cultureInfo = CultureInfo.GetCultureInfo(culture);

            var selector = new SelectElement(Browser.FindElement(By.Id("culture-selector")));
            selector.SelectByValue(culture);

            // That should have triggered a redirect, wait for the main test selector to come up.
            MountTestComponent<GlobalizationBindCases>();
            WaitUntilExists(By.Id("globalization-cases"));

            var cultureDisplay = WaitUntilExists(By.Id("culture-name-display"));
            Assert.Equal($"Culture is: {culture}", cultureDisplay.Text);

            // int
            var input = Browser.FindElement(By.Id("input_type_number_int"));
            var display = Browser.FindElement(By.Id("input_type_number_int_value"));
            Browser.Equal(42.ToString(cultureInfo), () => display.Text);
            Browser.Equal(42.ToString(CultureInfo.InvariantCulture), () => input.GetAttribute("value"));

            input.Clear();
            input.SendKeys(9000.ToString(CultureInfo.InvariantCulture));
            input.SendKeys("\t");
            Browser.Equal(9000.ToString(cultureInfo), () => display.Text);
            Browser.Equal(9000.ToString(CultureInfo.InvariantCulture), () => input.GetAttribute("value"));

            // decimal
            input = Browser.FindElement(By.Id("input_type_number_decimal"));
            display = Browser.FindElement(By.Id("input_type_number_decimal_value"));
            Browser.Equal(4.2m.ToString(cultureInfo), () => display.Text);
            Browser.Equal(4.2m.ToString(CultureInfo.InvariantCulture), () => input.GetAttribute("value"));

            input.Clear();
            input.SendKeys(9000.42m.ToString(CultureInfo.InvariantCulture));
            input.SendKeys("\t");
            Browser.Equal(9000.42m.ToString(cultureInfo), () => display.Text);
            Browser.Equal(9000.42m.ToString(CultureInfo.InvariantCulture), () => input.GetAttribute("value"));

            // datetime
            input = Browser.FindElement(By.Id("input_type_date_datetime"));
            display = Browser.FindElement(By.Id("input_type_date_datetime_value"));
            var extraInput = Browser.FindElement(By.Id("input_type_date_datetime_extrainput"));
            Browser.Equal(new DateTime(1985, 3, 4).ToString(cultureInfo), () => display.Text);
            Browser.Equal(new DateTime(1985, 3, 4).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), () => input.GetAttribute("value"));

            ReplaceText(extraInput, new DateTime(2000, 1, 2).ToString(cultureInfo));
            extraInput.SendKeys("\t");
            Browser.Equal(new DateTime(2000, 1, 2).ToString(cultureInfo), () => display.Text);
            Browser.Equal(new DateTime(2000, 1, 2).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), () => input.GetAttribute("value"));

            // datetimeoffset
            input = Browser.FindElement(By.Id("input_type_date_datetimeoffset"));
            display = Browser.FindElement(By.Id("input_type_date_datetimeoffset_value"));
            extraInput = Browser.FindElement(By.Id("input_type_date_datetimeoffset_extrainput"));
            Browser.Equal(new DateTimeOffset(new DateTime(1985, 3, 4)).ToString(cultureInfo), () => display.Text);
            Browser.Equal(new DateTimeOffset(new DateTime(1985, 3, 4)).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), () => input.GetAttribute("value"));

            ReplaceText(extraInput, new DateTimeOffset(new DateTime(2000, 1, 2)).ToString(cultureInfo));
            extraInput.SendKeys("\t");
            Browser.Equal(new DateTimeOffset(new DateTime(2000, 1, 2)).ToString(cultureInfo), () => display.Text);
            Browser.Equal(new DateTimeOffset(new DateTime(2000, 1, 2)).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), () => input.GetAttribute("value"));
        }

        [Theory]
        [InlineData("en-US")]
        [InlineData("fr-FR")]
        public void CanSetCultureAndParseCultureInvariantNumbersAndDatesWithFormComponents(string culture)
        {
            var cultureInfo = CultureInfo.GetCultureInfo(culture);

            var selector = new SelectElement(Browser.FindElement(By.Id("culture-selector")));
            selector.SelectByValue(culture);

            // That should have triggered a redirect, wait for the main test selector to come up.
            MountTestComponent<GlobalizationBindCases>();
            WaitUntilExists(By.Id("globalization-cases"));

            var cultureDisplay = WaitUntilExists(By.Id("culture-name-display"));
            Assert.Equal($"Culture is: {culture}", cultureDisplay.Text);

            // int
            var input = Browser.FindElement(By.Id("inputnumber_int"));
            var display = Browser.FindElement(By.Id("inputnumber_int_value"));
            Browser.Equal(42.ToString(cultureInfo), () => display.Text);
            Browser.Equal(42.ToString(CultureInfo.InvariantCulture), () => input.GetAttribute("value"));

            input.Clear();
            input.SendKeys(9000.ToString(CultureInfo.InvariantCulture));
            input.SendKeys("\t");
            Browser.Equal(9000.ToString(cultureInfo), () => display.Text);
            Browser.Equal(9000.ToString(CultureInfo.InvariantCulture), () => input.GetAttribute("value"));

            // decimal
            input = Browser.FindElement(By.Id("inputnumber_decimal"));
            display = Browser.FindElement(By.Id("inputnumber_decimal_value"));
            Browser.Equal(4.2m.ToString(cultureInfo), () => display.Text);
            Browser.Equal(4.2m.ToString(CultureInfo.InvariantCulture), () => input.GetAttribute("value"));

            input.Clear();
            input.SendKeys(9000.42m.ToString(CultureInfo.InvariantCulture));
            input.SendKeys("\t");
            Browser.Equal(9000.42m.ToString(cultureInfo), () => display.Text);
            Browser.Equal(9000.42m.ToString(CultureInfo.InvariantCulture), () => input.GetAttribute("value"));

            // datetime
            input = Browser.FindElement(By.Id("inputdate_datetime"));
            display = Browser.FindElement(By.Id("inputdate_datetime_value"));
            var extraInput = Browser.FindElement(By.Id("inputdate_datetime_extrainput"));
            Browser.Equal(new DateTime(1985, 3, 4).ToString(cultureInfo), () => display.Text);
            Browser.Equal(new DateTime(1985, 3, 4).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), () => input.GetAttribute("value"));

            ReplaceText(extraInput, new DateTime(2000, 1, 2).ToString(cultureInfo));
            extraInput.SendKeys("\t");
            Browser.Equal(new DateTime(2000, 1, 2).ToString(cultureInfo), () => display.Text);
            Browser.Equal(new DateTime(2000, 1, 2).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), () => input.GetAttribute("value"));

            // datetimeoffset
            input = Browser.FindElement(By.Id("inputdate_datetimeoffset"));
            display = Browser.FindElement(By.Id("inputdate_datetimeoffset_value"));
            extraInput = Browser.FindElement(By.Id("inputdate_datetimeoffset_extrainput"));
            Browser.Equal(new DateTimeOffset(new DateTime(1985, 3, 4)).ToString(cultureInfo), () => display.Text);
            Browser.Equal(new DateTimeOffset(new DateTime(1985, 3, 4)).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), () => input.GetAttribute("value"));

            ReplaceText(extraInput, new DateTimeOffset(new DateTime(2000, 1, 2)).ToString(cultureInfo));
            extraInput.SendKeys("\t");
            Browser.Equal(new DateTimeOffset(new DateTime(2000, 1, 2)).ToString(cultureInfo), () => display.Text);
            Browser.Equal(new DateTimeOffset(new DateTime(2000, 1, 2)).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), () => input.GetAttribute("value"));
        }

        // see: https://github.com/seleniumhq/selenium-google-code-issue-archive/issues/214
        //
        // Calling Clear() can trigger onchange, which will revert the value to its default.
        private static void ReplaceText(IWebElement element, string text)
        {
            element.SendKeys(Keys.Control + "a");
            element.SendKeys(text);
        }
    }
}
