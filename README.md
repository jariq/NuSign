NuSign
=============
**NuGet package signing prototype**

## What is NuSign ?

NuSign is a command line application that can digitally sign NuGet packages. It can also verify the signature of any previously signed NuGet package.

**WARNING: NuSign SHOULD NOT be used in production!** It is just a prototype tool which demonstrates that NuGet packages can include embedded signatures and still remain backwards compatible with already existing NuGet tools.

## Why NuGet packages need to be signed ?

NuGet package repository hosted at [nuget.org](https://www.nuget.org) is currently vulnerable to several attacks such as for example [typosquatting attack](https://github.com/NuGet/Home/issues/2974) because:

- anyone can submit package and it gets accepted automatically without the human review
- packages can indirectly execute arbitrary code on developer machine (via [MSBuild props and targets](https://docs.microsoft.com/en-us/nuget/create-packages/creating-a-package#including-msbuild-props-and-targets-in-a-package) present in a package) and all end-user machines (via .NET assemblies included in package)

## Does NuGet package signature make assembly signatures obsolete ?

No, it doesn't. Ideally all packages will use both signature types in the future.

Assembly signatures created with [SignTool](https://msdn.microsoft.com/en-us/library/windows/desktop/aa387764(v=vs.85).aspx) can be verified in the runtime (where NuGet package does not exist anymore) or manually by the end-user.

Package level signature created with [NuSign](https://github.com/jariq/NuSign) protects integrity and origin/authenticity of not only assemblies but also MSBuild props and targets and all the other files included in NuGet package. Package level signature can be verified during the development when package is added as a reference.

## How do I sign a package ?

Let's assume you have a valid code signing certificate present in your `CurrentUser\My` certificate store and you want to sign `MyPackage.1.0.0.nupkg`.

You can initiate package signing with following command:

    c:\> NuSign.exe --sign MyPackage.1.0.0.nupkg

NuSign will ask you to select certificate you want to use for signing...

![Pkcs11Interop architecture](doc/images/certpicker.png?raw=true)

...and will then use it to sign the package. If your signing certificate is stored in smartcard then you might also be prompted to enter PIN code.

Full command output looks like this:

    c:\> NuSign.exe --sign MyPackage.1.0.0.nupkg
    Signing package "MyPackage.1.0.0.nupkg"...
    Package "MyPackage.1.0.0.nupkg" successfully signed.

    Package was signed with the following certificate:
      Issuer:         CN=Certum Code Signing CA SHA2, OU=Certum Certification Authority, O=Unizeto Technologies S.A., C=PL
      Subject:        E=jaroslav.imrich@gmail.com, CN="Open Source Developer, Jaroslav IMRICH", O=Open Source Developer, C=SK
      Serial number:  78E0593A6F048F998C255F8BC2892D82
      Invalid before: Wed, 11 Jan 2017 07:52:59 GMT
      Invalid after:  Thu, 11 Jan 2018 07:52:59 GMT

Alternatively you can also perform signing operation in non-interactive/headless mode by specifying thumbprint of signing certificate with `--cert` parameter:

    c:\> NuSign.exe --sign MyPackage.1.0.0.nupkg --cert d5de31ea974f5ea8581d633eeffa8f3ea0d479bb

## How do I verify package signature?

Basic validation of package signature can be performed with the following command:

    c:\> NuSign.exe --verify MyPackage.1.0.0.nupkg

    Verifying the signature of package "NuSign.TestLibrary.Signed.1.0.0.nupkg" without the validation of signing certificate...
    Signature of "MyPackage.1.0.0.nupkg" package is VALID.

    Package was signed with the following certificate:
      Issuer:         CN=Certum Code Signing CA SHA2, OU=Certum Certification Authority, O=Unizeto Technologies S.A., C=PL
      Subject:        E=jaroslav.imrich@gmail.com, CN="Open Source Developer, Jaroslav IMRICH", O=Open Source Developer, C=SK
      Serial number:  78E0593A6F048F998C255F8BC2892D82
      Invalid before: Wed, 11 Jan 2017 07:52:59 GMT
      Invalid after:  Thu, 11 Jan 2018 07:52:59 GMT

If you want NuSign to perform also validation of signing certificate (whether it's been issued by the trusted CA and whether it hasn't been revoked) you will need to add `--performCertValidation` parameter:

    c:\> NuSign.exe --verify MyPackage.1.0.0.nupkg --performCertValidation

## Where is package signature stored ?

**TODO - Add technical details**

## Can NuSign.exe get merged into NuGet.exe ?

**TODO - Add checklist**