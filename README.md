# TagCleanup
Using taglib-sharp for ID3 information. Had to make a small change to make the ID3 v2 frames publically accessible.
This allowed me to check the frames and also remove specific ones not needed. For instance, PRIV, MCID (which is barely ever used right),
GEOB and Windows Media player frames. Most items are configured in the app.config.

I usually get to work on this in 30-45 minute increments while the baby naps, or eats, or does whatever a baby does. So, if you see something
that makes you eye twitch, the baby made me do it.

This is setup to use a MySQL database server. I could not find a portable database fast enough to handle the amount of data I was throwing at
it in a short period of time. SO in order to keep scans of 150,000+ files take less than a week, the server route was the only way to go...for now.

I'm open to suggestions.

This is a work in progress as I find out how bad my MP3 collection is named as stored.

Enjoy.