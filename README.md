AzureTranslationLib
===================

This is a class library to connect and query with Microsoft Azure Translation Service, created for .net 4.0 projects.

It is currently a simple library to translate text from english to one of the available languages in the Microsoft Translator API (http://datamarket.azure.com/dataset/bing/microsofttranslator). I am planning on implementing the language detection features and translations from any to any available language when I have time.

The codes for available languages are here: http://msdn.microsoft.com/en-us/library/hh456380.aspx

Implementation instructions:
1. Follow steps 1 and 2 to set up an Azure Marketplace account and get your clientID and clientSecret http://msdn.microsoft.com/en-us/library/hh454950.aspx
2. Input clientID and clientSecret under AzureTranslate.Translation.Execute() (or edit the function to pass it however you would like)
3. Build the project
4. Add the AzureTranslate.dll file you just created in /bin/ to wherever you put your class libraries in your project (I use /bin/ in my projects)
5. Add a reference to AzureTranslate.dll in Visual Studio, and import the AzureTranslate Namespace.
6. Add a SERVICE reference to http://api.microsofttranslator.com/V2/Soap.svc - Name it "TranslatorService" without the quotes (Important!)
7. Run Translation.Execute(text,language) and it will return a string translated!