<div style="text-align: center;">

[![Logo](/images/logo.png)](https://github.com/kazem-ma79/CFScanner)

<h1 style="text-align: center;">CF Domain Scanner</h1>

An awesome domain-based Cloudflare CDN IP scanner and domain filter checker! 

[**Explore the docs »**](https://github.com/kazem-ma79/CFScanner) 

[Report Bug](https://github.com/kazem-ma79/CFScanner/issues) . [Request Feature](https://github.com/kazem-ma79/CFScanner/issues)

![Downloads](https://img.shields.io/github/downloads/kazem-ma79/CFScanner/total) ![Forks](https://img.shields.io/github/forks/kazem-ma79/CFScanner?style=social) ![Stargazers](https://img.shields.io/github/stars/kazem-ma79/CFScanner?style=social) ![Issues](https://img.shields.io/github/issues/kazem-ma79/CFScanner) ![License](https://img.shields.io/github/license/kazem-ma79/CFScanner)

</div>

## Table Of Contents

*   [About the Project](#about-the-project)
*   [Built With](#built-with)
*   [Download](#download)
*   [Usage](#usage)
    *   [GUI (Windows/Android)](#gui-version)
    *   [CLI (Linux)](#cli-version)
*   [Contributing](#contributing)
*   [License](#license)
*   [Acknowledgements](#acknowledgements)

## About The Project

<img src="/images/sc-android.png" width="200px"/>
<img src="/images/sc-windows.png" width="280px"/>

Using this tool you can scan Cloudflare CDN IP addresses that work with your own domain. Also it's possible to check if your domain is filtered.

*   Domains that are filtered will work on some Cloudflare IPs (clean IP) and not work on some others
*   You can find which IP works on your domain name
*   No need to scan all IP ranges, CDN IP addresses are included
*   Project available on multiple platforms (Windows, Linux, Android, ...)
*   Ping speed of IPs to find the fastest one

## Built With

The project is built using multiple frameworks to work on more platforms.

Framework/Languages:

*   C# (WPF/Xamarin)
*   Python

Platforms:

*   Windows (GUI/CLI)
*   Linux (CLI)
*   Android (GUI)

## Download

Get the latest version from [github](https://github.com/Kazem-ma79/CFScanner/releases/latest).

## Usage

This is an easy-to-use tool whose GUI version works with 1-click and CLI version with no-click :smile:.

### GUI Version

**Scan**

*   Enter your domain (subdomain) in the hostname field
*   Enter port number (v2ray port)
*   Set thread count (10-100 recommended on android and 50-200 for Windows/Linux based on system resources and network connection)
*   Click `Scan` button

**Filter Check**

*   Enter your domain (subdomain) in hostname field
*   Enter port number (v2ray or x-ui port)
*   Enter Cloudflare IP you want to check domain on
*   Click `Check` button

### CLI Version

Simply run the following command:  
**Scan**  
cfscanner.py --mode scan --domain example.com --port 443 --thread 50  
**Check**  
cfscanner.py --mode check --domain example.com --port 443 --ip 1.2.3.4

#### Arguments:

*   mode: Action type to scan or check domain. `scan`/`check`
*   domain: Domain name/Subdomain.
*   port: Port number of v2ray to scan and v2ray/x-ui to check domain filter.
*   thread: Thread count to run app faster (only for scanner)
*   ip: Cloudflare CDN IP address to check domain on it (only for filter checking)

## Contributing

Contributions are what make the open-source community such an amazing place to learn, inspire, and create. Any contributions you make are **greatly appreciated**.

*   If you have suggestions for adding or removing projects, feel free to [open an issue](https://github.com/kazem-ma79/CFScanner/issues/new) to discuss them.
*   Create PR for suggestions.

## License

Distributed under the MIT License. See [LICENSE](https://github.com/kazem-ma79/CFScanner/blob/main/LICENSE.md) for more information.

## Acknowledgements

*   [Cloudflare IP Ranges](https://ircf.space/)
*   [GFW-Knocker](https://github.com/GFW-knocker/)

> #اینترنت\_برای\_همه\_یا\_هیچکس
