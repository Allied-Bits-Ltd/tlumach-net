You are helping a developer add new tests to the Tlumach test library located in the C:\Projects\Tlumach\tlumach-net\tests directory and its subdirectories. The test data for the tests is found in the directories named TestData within the test library; the test data are grouped by the translation file format.

I have added the new classes that let one write translation files from the Translation objects, loaded from a file. In other words, one can load a translation into a translation object or objects via the TranslationManager class and then same this or these translations in another format using the newly created writer classes. 
The writer classes reside in the Tlumach.Writers project in the C:\Projects\Tlumach\tlumach-net\src\Tlumach.Writers directory. 

Please inspect, for which formats the writers exist. The final writer class is the one that has the "FormatName" property return some value which is the name of the format the writer uses to write the translation. 

Please study how existing tests load the data, then create a set of tests that will verify writing to a different format. Place the tests into the C:\Projects\Tlumach\tlumach-net\tests\Tlumach.WriterTests\ directory and the corresponding project. 

The set of steps in each test should look like this:
1) load some translations from some format X. The tests can reuse the test data from the C:\Projects\Tlumach\tlumach-net\tests\Tlumach.Tests\TestData\ directory and its subdirectories. If you need more test data, feel free to generate new files and place it to the corresponding subdirectory based on the format. Note that the GetTranslation method of TranslationManager by default returns the loaded translation but does not load one if it is not present. You should specify the tryLoadExisitng parameter of this method to make it load a translation.
2) write the translation to the format being tested.
3) where possible (JSON- and XML-based formats), check the 
4) load back the translation from the file written on step 2 and compare the loaded translation or translations with the translations loaded on step 1.  

Add some single shared method for comparing translations, so that all tests could use it. 

The user's additional comments to this request are: $ARGUMENTS

-----------------------

I have added the new classes that let one write translation files from the Translation objects, loaded from a file. In other words, one can load a translation into a translation object or objects via the TranslationManager class and then same this or these translations in another format using the newly created writer classes. 
The writer classes reside in the Tlumach.Writers project in the C:\Projects\Tlumach\tlumach-net\src\Tlumach.Writers directory. 

Please inspect, for which formats the writers exist. The final writer class is the one that has the "FormatName" property return some value which is the name of the format the writer uses to write the translation. There exist writers for INI, TOML, and simple JSON formats. 

I need you to add a set of classes to write CSV and TSV files, i.e. comma-separated and tab-separated files. The approach should be as follows:
1) create the BaseTableWriter class similar to BaseKeyValueWriter and BaseJsonWriter classes. 
2) create the CsvWriter and TsvWriter classes which are the descendants of the BaseTableWriter class. 
3) the writers do the job opposite to parsers. You will find the CSV and TSV parsers in C:\Projects\Tlumach\tlumach-net\src\Tlumach.Base\CsvParser.cs and C:\Projects\Tlumach\tlumach-net\src\Tlumach.Base\TsvParser.cs respectively, with their common ancestor being in C:\Projects\Tlumach\tlumach-net\src\Tlumach.Base\BaseTableParser.cs 
4) CsvWriter should have a SeparatorChar property similar to the SeparatorChar property of CsvParser. This property specifies which character to use for a separator. 

Please put the new classes into the corresponding files in the  C:\Projects\Tlumach\tlumach-net\src\Tlumach.Writers directory.


-----------------------

I have added the new classes that let one write translation files from the Translation objects, loaded from a file. In other words, one can load a translation into a translation object or objects via the TranslationManager class and then same this or these translations in another format using the newly created writer classes. 
The writer classes reside in the Tlumach.Writers project in the C:\Projects\Tlumach\tlumach-net\src\Tlumach.Writers directory. 

Please inspect, for which formats the writers exist. The final writer class is the one that has the "FormatName" property return some value which is the name of the format the writer uses to write the translation. There exist writers for INI, TOML, simple JSON, and CVS and TSV formats. 

I need you to add a set of classes to write .NET .resx files that use XML. The approach should be as follows:
1) create the BaseXmlWriter class similar to BaseKeyValueWriter, BaseJsonWriter, and BaseTableWriter classes. 
2) create the ResxWriter class which is the descendant of the BaseXmlWriter class. 
3) the writer does the job opposite to a parser. You will find the resx  parser in C:\Projects\Tlumach\tlumach-net\src\Tlumach.Base\ResxParser.cs and its ancestor in C:\Projects\Tlumach\tlumach-net\src\Tlumach.Base\BaseXMLParser.cs 

Please put the new classes into the corresponding files in the  C:\Projects\Tlumach\tlumach-net\src\Tlumach.Writers directory.

---------------
Please update the claude.md file with your new knowledge about the writer classes that you can find in 
