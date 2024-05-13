CodeKicker.BBCode.Core
=====================

This is a fork of the [original modification repository](https://github.com/Pablissimo/CodeKicker.BBCode-Mod).

## What's new
* fully compatible with netstandard 2.1, net 6.0, net 8.0
* supports the bbcode uid feature of PHPBB 3.x. This feature is not fully documented anywhere, but [this forum thread](https://www.phpbb.com/community/viewtopic.php?t=1378765) should be a good place to start.
* supports the bbcode [bitfield](https://www.phpbb.com/support/docs/en/3.1/kb/article/how-to-template-bitfield-and-bbcodes/) feature of PHPBB 3.x
* fully backwards compatible with PHPBB 3.x (call `BBCodeParser.TransformForBackwardsCompatibility` on your own BBCode text to generate BBCode that can be parsed by a phpbb 3.0 engine)
* added support for nested tags in list items
* added support for phpbb 3.x inline attachment html comments
* made whitespace management fully compatible with phpbb3 

## Usage
Install the [NuGet package](https://www.nuget.org/packages/CodeKicker.BBCode.Core/)

This project is used in [PhpbbInDotNet](https://github.com/costinbanu/PhpbbInDotnet).

## License
Both the original project and the modification repo are licensed under the MIT license, thus my changes are released under the same terms.
