abot [![Build Status](https://ci.appveyor.com/api/projects/status/b1ukruawvu6uujn0?svg=true)](https://ci.appveyor.com/project/sjdirect/abot)
====

C# web crawler build for speed and flexibility.

Abot is an open source C# web crawler built for speed and flexibility. It takes care of the low level plumbing (multithreading, http requests, scheduling, link parsing, etc..). You just register for events to process the page data. You can also plugin your own implementations of core interfaces to take complete control over the crawl process.

Abot targets .NET version 4.0.

  * [Ask questions and search for answers on the Community Forum](http://groups.google.com/group/abot-web-crawler)<br />
  * [Report Bugs or Suggest Features](https://github.com/sjdirect/abot/issues)
  * [Consider making a donation](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=G6ZY6BZNBFVQJ)

###What's So Great About It?
  * Open Source (Free for commercial and personal use)
  * Speed
  * Every part of the architecture is pluggable
  * Heavily unit tested (High code coverage)
  * Very lightweight (not over engineered)
  * No database required
  * Easy to customize (When to crawl a page, when not to crawl, when to crawl page's links, etc..)
  * Runs on Mono
