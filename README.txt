Inside the project folder you will see a directory named 'Params'. This contains a JSON file named 'params.json'. You put all your predefined search parameters inside this file.

**** THINGS YOU ARE NOT MEANT TO DO ****
1. Don't re-structure the file.
2. Don't change the json schema.
3. Don't manipulate json property names.

**** THINGS YOU SHOULD BE AWARE OF ****
This JSON doc is competely structured & meant to contain string data. Putting different data type such as bool, integers, objects might make the whole thing upset.

**** PUTTING OWN SEARCH PARAMETERS ****
1. "keywords" -> Is the list of search keywords. You can put things like this ["airbnb england", "uber employees", "tesla"]. Here every string inside "" is a full keyword & they are seperated by a comma ','.

2. "invitation_message" -> This contains the custom message you want to send everytime when this script sends a connection request. Message format should be something like: 

"Hello I found via search blah blah blah. I'd like to connect you.".

The above text gets formatted to:

"Hello Alex,
I found via search blah blah blah. I'd like to connect you."

3. "followup_message" -> Pretty similar to previous one. Used to send a follow up to recently added connection.

4. "search_type" -> Determines what do you want to search. Available options are "people", "content" & "jobs". Putting anything else will create gibberish.

5. "location" -> Determines the place based on where you want to see you search results.

Available locations for now: 

"Australia", "Belgium", "Brazil", "Canada", "China", "Denmark", "France", "Finland", "Germany", "Israel", "Italy", "India", "Japan", "Netherlands", "New Zealand", "Norway", "Poland", "Russia", "Romania", "Sweden", "Spain", "Switzerland", "United States", "United Kingdom"

NOTE:- YOU HAVE TO USE THE LOCATION NAME AS IS PROVIDED HERE.