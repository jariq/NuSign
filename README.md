NuSign
=============
**NuGet package signing prototype**

## Table of Contents

* [Overview](#overview)
* [Why do NuGet packages need to be signed?](#why-do-nuget-packages-need-to-be-signed)
* [Does NuGet package signature make assembly signature obsolete?](#does-nuget-package-signature-make-assembly-signature-obsolete)
* [How do I sign a package?](#how-do-i-sign-a-package)
* [How do I verify package signature?](#how-do-i-verify-package-signature)
* [How does the signing work?](#how-does-the-signing-work)
* [How does signature verification work?](#how-does-signature-verification-work)
* [What is the fastest way to explore NuSign?](#what-is-the-fastest-way-to-explore-nusign)
* [What is going to happen next?](#what-is-going-to-happen-next)
* [About](#about)

## Overview

NuSign is a command line application that digitally signs NuGet packages. It can also verify the signature of any previously signed NuGet package.

> **WARNING: NuSign is just proof of the concept which SHOULD NOT be used in production!**  

All NuGet package signing related resources I've managed to find are rather old:
- More than a year old [package signing issue](https://github.com/NuGet/Home/issues/2577) open in NuGet repository
- More than two years old [package signing post](http://blog.nuget.org/20150203/package-signing.html) post on blog.nuget.org
- Deprecated [package signing specification](https://github.com/aspnet/Signing/blob/dev/Spec.md) in ASP.NET GitHub repository

I've decided to create NuSign hoping its existence might help revive both upstream discussion and upstream implementation of NuGet package signing.

I've followed these simple design goals:
- signature needs to be embedded in the package so it does not get lost and can be verified even on packages imported to local repositories
- signed package needs to be backwards compatible so it can be consumed by the client tools without the support for package signatures
- signing technology needs to be based on X.509 code-signing certificates just like like assembly signatures are

## Why do NuGet packages need to be signed?

NuGet package repository hosted at [nuget.org](https://www.nuget.org) is currently vulnerable to several attacks such as [typosquatting attack](https://github.com/NuGet/Home/issues/2974) because:

- anyone can submit package and it gets accepted automatically without any kind of human review
- packages can indirectly execute arbitrary code
  -  on developer machine via [MSBuild props and targets](https://docs.microsoft.com/en-us/nuget/create-packages/creating-a-package#including-msbuild-props-and-targets-in-a-package) present in a package
  - on end-user machine via .NET assemblies included in package

## Does NuGet package signature make assembly signature obsolete?

No, it does not. Ideally all packages will use both signature types in the future.

Assembly signature created with [SignTool](https://msdn.microsoft.com/en-us/library/windows/desktop/aa387764(v=vs.85).aspx)
protects integrity and origin/authenticity of .NET assembly (.dll file). It can be verified in the runtime (where NuGet package does not exist anymore) or manually by the end-user.

Package signature created with [NuSign](https://github.com/jariq/NuSign) protects integrity and origin/authenticity of all files present in the NuGet package (assemblies, MSBuild props and targets and all the other files). Package level signature can be verified during the development when package is added as a reference.

## How do I sign a package?

Let's assume you have a valid code signing certificate present in your `CurrentUser\My` certificate store and you want to sign `MyLibrary.1.0.0.nupkg`. You can initiate package signing with following command:

    c:\> NuSign.exe -sign MyLibrary.1.0.0.nupkg
    Signing package "MyLibrary.1.0.0.nupkg"...
    Package "MyLibrary.1.0.0.nupkg" successfully signed.

    Package was signed with the following certificate:
      Issuer:         CN=Certum Code Signing CA SHA2, OU=Certum Certification Authority, O=Unizeto Technologies S.A., C=PL
      Subject:        E=jaroslav.imrich@gmail.com, CN="Open Source Developer, Jaroslav IMRICH", O=Open Source Developer, C=SK
      Serial number:  78E0593A6F048F998C255F8BC2892D82
      Invalid before: Wed, 11 Jan 2017 07:52:59 GMT
      Invalid after:  Thu, 11 Jan 2018 07:52:59 GMT

NuSign will display GUI dialog which will let you select certificate you want to use for signing. If your signing certificate is stored in the smartcard then you might also be prompted to enter PIN code.

Alternatively you can perform signing operation in non-interactive/headless mode by specifying thumbprint of signing certificate with `-cert` parameter:

    c:\> NuSign.exe -sign MyLibrary.1.0.0.nupkg -cert d5de31ea974f5ea8581d633eeffa8f3ea0d479bb

## How do I verify package signature?

Validation of package signature can be performed with the following command:

    c:\> NuSign.exe -verify MyLibrary.1.0.0.nupkg
    Verifying the signature of package "NuSign.TestLibrary.Signed.1.0.0.nupkg"...
    Signature of "MyLibrary.1.0.0.nupkg" package is VALID.

    Package was signed with the following certificate:
      Issuer:         CN=Certum Code Signing CA SHA2, OU=Certum Certification Authority, O=Unizeto Technologies S.A., C=PL
      Subject:        E=jaroslav.imrich@gmail.com, CN="Open Source Developer, Jaroslav IMRICH", O=Open Source Developer, C=SK
      Serial number:  78E0593A6F048F998C255F8BC2892D82
      Invalid before: Wed, 11 Jan 2017 07:52:59 GMT
      Invalid after:  Thu, 11 Jan 2018 07:52:59 GMT

If you want NuSign to skip validation of signing certificate (whether it's been issued by the trusted CA and whether it hasn't been revoked) you will need to add `-skipCertValidation` parameter:

    c:\> NuSign.exe -verify MyLibrary.1.0.0.nupkg -skipCertValidation

## How does the signing work?

NuGet package (`.nupkg` file) is just an ordinary ZIP archive (`.zip` file) with the following directory structure:

![Structure of unsigned NuGet package](images/unsigned_package.png?raw=true)

During the signing phase NuSign first creates file `package/signatures/IntegrityList.xml` which contains list of all other files present in the package along with their cryptographic hashes:

```xml
<?xml version="1.0" encoding="utf-8"?>
<IntegrityList xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
  <IntegrityEntry>
    <FilePath>lib/netstandard1.0/MyLibrary.dll</FilePath>
    <HashAlgorithm>http://www.w3.org/2000/09/xmldsig#sha256</HashAlgorithm>
    <HashValue>xODa4SUb8nlwSCNVhQdbNcz7W69008nUtMPPsVkIr4U=</HashValue>
  </IntegrityEntry>
  <IntegrityEntry>
    <FilePath>MyLibrary.nuspec</FilePath>
    <HashAlgorithm>http://www.w3.org/2000/09/xmldsig#sha256</HashAlgorithm>
    <HashValue>qPH6r27f5UdmKOlQIG7NjSZ6y3/2GOG3E5CruBknuYc=</HashValue>
  </IntegrityEntry>
  <IntegrityEntry>
    <FilePath>package/services/metadata/core-properties/11faa6a592fc481090e9683c5b96d7bd.psmdcp</FilePath>
    <HashAlgorithm>http://www.w3.org/2000/09/xmldsig#sha256</HashAlgorithm>
    <HashValue>NvSHHsSrJxMw1eGMqA1mbtxj06CFUF6bCmJh6Z4hd1I=</HashValue>
  </IntegrityEntry>
  <IntegrityEntry>
    <FilePath>[Content_Types].xml</FilePath>
    <HashAlgorithm>http://www.w3.org/2000/09/xmldsig#sha256</HashAlgorithm>
    <HashValue>pZlrOm75NHkBx0F3V4ZX1tGJUxxhvGu+eKANLHevbzo=</HashValue>
  </IntegrityEntry>
  <IntegrityEntry>
    <FilePath>_rels/.rels</FilePath>
    <HashAlgorithm>http://www.w3.org/2000/09/xmldsig#sha256</HashAlgorithm>
    <HashValue>tvI1YA6lHdnIcQyXGPU7LCZTVNz8y1R6M1IaKnxd130=</HashValue>
  </IntegrityEntry>
</IntegrityList>
```

File `package/signatures/IntegrityList.xml` is then signed with selected signing certificate and resulting [CMS signature](https://tools.ietf.org/rfc/rfc5652.txt) is stored in DER encoded (binary) form in file `package/signatures/IntegrityList.p7s`.

That's it. Final signed package with following directory structure seems to be fully compatible with already existing NuGet tools and also with nuget.org repository:

![Structure of signed NuGet package](images/signed_package.png?raw=true)

## How does signature verification work?

In the first phase of package signature verification NuSign validates CMS signature of `IntegrityList.xml` file. It uses Windows certificate store as a source of information about trusted certificate authorities.

In the second phase NuSign computes cryptographic hash for every file present in the package except those in `signatures` folder. Then it compares computed hashes with the hashes stored in `IntegrityList.xml` file.

Package signature is considered to be valid only if CMS signature is valid, all hashes match and there is no missing or additional file present in the package.

## What is the fastest way to explore NuSign?

There is a precompiled `NuSign.exe` binary available in [samples directory](samples) along with the following demonstration packages:

- [NuSign.TestLibrary.Unsigned](samples/NuSign.TestLibrary.Unsigned.1.0.0.nupkg?raw=true) - unsigned package [available also on nuget.org](https://www.nuget.org/packages/NuSign.TestLibrary.Unsigned/)
- [NuSign.TestLibrary.Signed](samples/NuSign.TestLibrary.Signed.1.0.0.nupkg?raw=true) - signed package [available also on nuget.org](https://www.nuget.org/packages/NuSign.TestLibrary.Signed/)
- [NuSign.TestLibrary.Tampered](samples/NuSign.TestLibrary.Tampered.1.0.0.nupkg?raw=true) - signed package with tampered content [available also on nuget.org](https://www.nuget.org/packages/NuSign.TestLibrary.Tampered/)

## What is going to happen next?

NuSign can be improved in many ways:

- support for other platforms than Windows can be added
- support for external trust sources can be added
- support for PKCS#11 compatible key stores can be added
- support for signature time-stamps can be added
- use of code-signing certificates could/should be enforced
- ...

However I'd like to discuss high level design and technical details with NuGet upstream before I take on the burden of maintaining NuSign as a standalone tool. I hope that package signing will become an integral part of NuGet tool. Meanwhile please feel free to [open new issue](https://github.com/jariq/NuSign/issues/new) to report bugs, request new features or just discuss your thoughts.

## About

NuSign has been written by [Jaroslav Imrich](https://www.jimrich.sk) and is available under the terms of [MIT license](LICENSE).