### Version v2.0.2 ###
  * Enabled matching episodes for shows with 10+ seasons (ie: episode #1302 is Season 13, epiosde 2)
  * Disabled absolute episode number in config file.
  * Added logic to not remove failed programs from the queue based on subtitle when subtitle was blank

### Version v2.0.1 ###
  * Language bug fix when looking up TheTVDB

### Version v2.0.0 ###
  * Compatible with Argus-TV 2.x

### Version v1.xxx ###
...

### Version v1.6.2 ###
  * Working with FTR 1.6 final
  * Code enhancements for more stability
### Version v1.5.1 ###
  * for FTR 1.5.x
  * calls to update show data will be done in chunks instead of one big call.
  * new config item: "maxShowNumberPerUpdate"  Use this to configure the size of the chunks.  The default 20 should be fine.
### Version v0.94: ###
  * Updated to work with FTR 1.6 RC.  Only upgrade if you are running 1.6 for now.
### Version v0.93: ###
  * Added a cache for series lookups to improve performance and reduce hits to thetvdb.com.
  * fixed a bug where long log messages would crash the service
### Version v0.91: ###
  * I had a network outage at my house and an exception was thrown from an underlying library that caused GuideEnricher to crash.  I added a small fix to log the error but continue running with these types of errors.
  * If a ; exists in the title, I take the portion before it and search based on just that string.  This happens with a lot of kids shows with multiple episodes per recording.  In the future, I will append all episodes, but for now it's just going to match the first.
### Version v0.9: ###
  * Added multi-language support - Thanks go to malingo for this!!!  See the config file for a new item that is commented out by default but can be uncommmented to change the language used to search thetvdb.com.
  * All episodes for a given schedule are now updated.  This is key to prevent conflicts from having different episode numbers for the same episode.
  * Remove the noisy log messages and leave just summary messages
  * Prevent the duplicate lookups to thetvdb.com
### WITHDRAWN!!! Version v0.8: ###
  * DO NOT USE version 0.8... only one download was done
  * BUG: It's updating 3500 or so entries in my guide... not exactly what I wanted to have happen.... looking into it... I'll re-release it when I get it more stable.... sorry for the inconvenience.
  * Added multi-language support - Thanks go to malingo for this!!!  See the config file for a new item that is commented out by default but can be uncommmented to change the language used to search thetvdb.com.
  * The enriching will now enrich all instances of the show in the guide.  For example, the process will start by finding all upcoming recordings.  Say, the first show is Fringe on channel 36 at 9PM Monday.  Previous versions would just update that single show in the guide.  Now, all episodes of Fringe on all channels are updated.
  * The new enriching will take longer but is needed to allow for proper scheduling.  If only some of the shows have SxxExx and others have default episode number information, FTR sees the episodes as different and thinks they both need to be recorded in some cases.
### Version v0.7: ###
  * Added the ability to ignore series.. say, a news daily or a series that is not likely going to have a complete set of data on thetvdb.com.
  * fixed a bug in the config file... there was an errorneous 

&lt;startup&gt;

 tag at the top that prevented the service to start.
### Version v0.6: ###
  * ANNOYING:  this version still shows as v0.4 in windows installer database... next version will be fixed ;)
  * Just got rid of the ForTheRecord ERROR log message when a series could not be found.  This would result in an email being sent.  Nothing in GuideEnricher is that important :).
### Version v0.5: ###
  * Installer no longer starts the service on install.... this allows the user a chance to change the configuration before starting the service
  * Added Regular Expression matching to the schedules direct series name.... In some cases, the series name in your guide data contains the episode name too.  Use the Regular Expression to remove the episode name and get the proper series match.  See config file for details.
### Version v0.4: ###
  * Fixed a bug with integer conversion
  * Changed log level of messages in console
  * FTR 1.5.0.3 now sends event when new guide data is retrieved with TVGuideImporter.  So, the sleeper thread is disabled by default but can be re-enabled by changing the value in config file to something other than 0.
  * Episode Number Display is now available in the file format in FTR ... verified that this works with Guide Enricher.  So a simpler file format is:
`%%TITLE%%\%%TITLE%% - %%EPISODENUMBERDISPLAY%% - %%EPISODETITLE%% - %%DATE%%_%%HOURS%%-%%MINUTES%%`
### Version v0.3: ###
  * Added FTR log events so you can see activity in the Managment Console
  * Added another thread called Sleeper.... this thread will wake up every 12 hours and enrich the guide data... this is necessary for those who have guide sources that don't generate guide events.
  * Started updating SeriesNumber nad EpisodeNumber fields in the database.... until the next FTR release, this is the only way to get the informtaion in the filenames... make your filename format something like this:
`%%TITLE%%\%%TITLE%% - S%%SERIES2%%E%%EPISODENUMBER2%% - %%EPISODETITLE%% - %%DATE%%_%%HOURS%%-%%MINUTES%%`


### Version 0.2: ###
  * Fixed registration with FTR to receive events

### Version 0.1: ###
  * First release