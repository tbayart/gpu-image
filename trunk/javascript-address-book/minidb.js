// JavaScript Document
/* ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
minidb implements a VERY simplified database query mechansim.

approprate use for minidb
~~~~~~~~~~~~~~~~~~~~~~~~
minidb is suitable for fairly small single table databases (up to a few thousand records),
with low update rates (every few seconds or lower).
It should be very simple to deploy compared to a full database solution;
but cannot provide all the functions of such a system.

outline deployment
~~~~~~~~~~~~~~~~~~
The following files are needed to make a complete minidb installation
these files should all be deployed on a web server.
	data$fid	a database file
	???.html:  		a host file, that embeds minidb.js, and contains format information for a list form of the database (details below)
	minidb.js:  	this file, which includes most of the searching and other code
	addr.php:		allows for updates to the database at the server; under direction from minidb.js
	filetime.php	allows for polling of the server by minidb.js to check for updates by another client

minidb.js, addr.php and filetime.php are files provided by the minidb system.	
data$fid and ???.html must be provided by the deployers of the application.

All of these files except minidb.js should be in the same 'host' directory;
mindidb.js may be at any location that can be referenced from the host directory.
addr.php and filetime.php are not needed if the database is static; 
or updated by some other mechanism such as offline update and periodic upload to the server.

end user options
~~~~~~~~~~~~~~~~
When the end user enters the deployed ???.html page, the page as defined by the deployer will be displayed,
including a summary listing of the entire database.

The page should provide a search input box; 
as the user types into this box the data is refined and a new summary listing is displayed.
(The update of the listing may be on each keystroke, or by clicking a button, depending how the deployer has set up the page).

The search is fairly freeform, covering all the fields of each record (whether or not that field is displayed in the list summary).
The search is a set of blank separated terms; with the result being the records that match ALL the terms.
There is some structure in the search for more exact search (to be written up; and subject to change).
We may add more options later for records that match ANY term, and other flexibilities.

The deployer may also provide a form version of the data; this displays all fields for a single record.
The user clicks on any record in the summary listing, and the details are displayed on the form.

The deployer may provide an 'Update' button associated with the form.
When the user changes values in the displayed form and clicks this 'Update' button,
the original record at the server is replaced with updated version.
However, if the record has already been changed by some other user, the update will be rejected.
Conflicting updates are pretty unlikely in the expected style of use:
a more sophisticated mechanism would be needed if they were expected to be at all common.

There could be multiple similar pages giving access to the same data:
with the different version displaying different summary data in different styles;
or providing different update options.


what the deployer must provide
~~~~~~~~~~~~~~~~~~~~~~~~~~
The deployer must provide two files: data$fid and ???.html.

The 'database' is retrieved from a server file defined by the variable fid ("addr.txt" -> file "dataaddr.txt").
This should be a file of lines separated by \n.  
Each line is a record; with fields separated by tabs \t.
The first line contains field names; these should be able to contain most characters including space, but not incuding '$', \n or \t.
It is possible to export such a file from a simple Excel spreadsheet.

The application is started from ???.html (name ??? chosed by deployer).
???.html is a normal webpage; and contains any required standard framework appropriate to the site.
To work with minidb ???.html must also contain
	script reference to minidb.js: eg '<script type="text/javascript" src="minidb.js"></script>'
	a input field with id="search": eg '<input type="text" id="search" name="search" size="50%" onkeyup="dbsubmit()" />'
	a '<div id="holder">' section that defines the display format for the listing of selected records. (details below)
		This allows the deployer to choose both what information is displayed in the summary listing, and its format.
	
???.html may optionally contain
	a button for submitting the query: eg '<input type="submit" onclick="dbsubmit()" value="Submit" />'
		The dbsubmit() method applies the query in the serch field and redisplays the output appropriately.
		dbsubmit() may be called frm a key method on the id="search" input, from a button, or both.
	a message are (id="message") for error output: eg '<p id="message"></p>'
		If this is not included, error messages are not displayed.
	a debug message are (id="dmessage") for debug output: eg '<p id="dmessage"></p>'
		If this is not included, messages are not displayed.
	a field (id="refreshrate") to define the interval with which the client will poll the host for changes (millisec)
	    If this is not included, the rate will be once per minute (60000)
	a div (id="form") which will display all the data for a record selected from the list in a standard form
		minidb does not currently provide deployer control over the format of this form.
	a button that will update the database with values the end user places in the form: 
		eg '<input type="submit" onclick="update()" value="Update" />'
		CURRENT TEMPORARY LIMITATION: this allows for update; minidb does not have insert/delete capability.
	
There may be several different files ???.html; providing different display styles and different update capability.
CURRENTLY all the ???.html files must be in the same directory as the source data.
This will have to change (easy change) to allow directory based security to different capabilities.

how to define the summary listing
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
... tbd

implementation details
~~~~~~~~~~~~~~~~~~~~~~
All query is implemented at the client; by code within minidb.js.

... tbd




~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ */
'use strict';

// constant globals
var fid = "addr.txt";  // name of the server file (without the 'data')

// globals fixed for life of session
var searchId = document.getElementById('search');
var browserId = document.getElementById('browser');
var holderId = document.getElementById('holder');
var messageId = document.getElementById('message');
var dmessageId = document.getElementById('dmessage');
var formId = document.getElementById('form');
var refreshrateId = document.getElementById('refreshrate');
var hitsId = document.getElementById('hits');
var insertId = document.getElementById('insertk');
var deleteId = document.getElementById('deletek');
var updateId = document.getElementById('updatek');
var clearId = document.getElementById('cleark');


// globals that change
var addr;  // the object array representing the entire database, addr[null] is an object with all blank fields
var currentXxid = null;   // the
function oldVersion() { return addr[currentXxid]; }
//var oldVersion;  // the original line value for the entry currently shown on the form

// Implementation notes:
//
// Internet Explorer failed to used cached formatted elements.
// Each element could be displayed once, but when redisplayed was empty.
// This was avoided with special case code for IE.
// However, it turned out that caching was not a significant benefit even on the other browsers
// and so is removed. 
//
// We originally prepared the HTML for each data line as a DOM element, and assembled them as children into the main 'holder'.
// However, it is slightly easier and just as fast on most browsers to prepare as a long HTML string, and set holder's innerHTML.
// The format and collection of the HTML string is very quick; it is setting innerHTML that is time conssuming.
// Presumably as this has the lower level browser parse and format.

// extract querystring, from http://stackoverflow.com/questions/901115/get-query-string-values-in-javascript
var urlParams = {};
(function () {
    var e,
        a = /\+/g,  // Regex for replacing addition symbol with a space
        r = /([^&=]+)=?([^&]*)/g,
        d = function (s) { return decodeURIComponent(s.replace(a, " ")); },
        q = window.location.search.substring(1);

    while (e = r.exec(q))
       urlParams[d(e[1])] = d(e[2]);
})();


// add trim() if not there - IE to blame as usual
// http://stackoverflow.com/questions/2308134/trim-in-javascript-not-working-in-ie
if(typeof String.prototype.trim !== 'function') {
  String.prototype.trim = function() {
    return this.replace(/^\s+|\s+$/g, ''); 
  }
}

// when ready, initialize the table, and do a query string search if any
window.onload = function() {
	window.focus();
	searchId.focus();
	navigator.app
	prepareFormat();

	var param = urlParams["q"];
	if (param != undefined) searchId.value = urlParams["q"];
	refresh();
	selectReady();
	setTimeout("onload2()", 0);
}
function onload2() {
	generateFormFromHeads();
	if (browserId != null) browserId.innerHTML = navigator.appName;
	//autoRefresh();
	refreshrateChange();
}

// refresh the values from the source file using current filter
function refresh() {
	// this only shows in Opera.  We would need more careful async to work in other browsers.
	holderId.innerHTML = '<p><font style="color: red; font-weight:bold;">Reading address data from server ...</font></p>';
	setTimeout("refresh2()", 0);
}

function refresh2() {
	addr = readfile();
	resort();
	dbsubmit();
}


// skel is an array of format items; one item for each line in the initial 'holder'
// each item contains 
//     parts: an array of strings which are alternating constants and field names
//     unset: which is what the substitution will look like if all parts are blank
// eg for the line:   '<br>mob: <b>$mobile$</b>'    we have
//     parts = ['<br>mob: <b>', 'mobile', '</b>']
//     unset = '<br>mob: <b></b>'
var skel;  

// read the format from the holder, parse and save into skel
function prepareFormat() {
	skel = [];
	var flines = holderId.innerHTML.split("//");  // split into lines; used to use \n, but IE8 does not handle it properly
	for (var l in flines) {
		var fline = flines[l];
		var parts = fline.split("$");  									      // split line into parts
		var compareline = parts.length == 1 ? "" : substituteBlanks(parts);   // what will it look like if all subs blank
		skel[l] = {parts: parts, unset: compareline};
	}
}

// substitute fields from line for odd numbered elements in string array parts
function substituteFields(line, parts) {
	var s = "";
	for (var i=0; i<parts.length-1; i+= 2) {
		s += parts[i];
		s += line[parts[i+1]];
	}
	s += parts[parts.length-1];
	return s;
}

// Substitute blanks for odd numbered elements in parts
// This is used to prepare a comparison, so we will know where all substituations were blanks
function substituteBlanks(parts) {
	var s = "";
	for (var i=0; i<parts.length-1; i+= 2) {
		s += parts[i];
	}
	s += parts[parts.length-1];
	return s;
}

// format a data line using the skeleton skel
// return as an html string, with surrounding span to enable line selection
function format(line) {
	var s = '<span class="entry" onclick="entryClicked(' + line.xxid + ')"><span id="entry_' + line.xxid + '">';
	for (var i in skel) {
		var sline = substituteFields(line, skel[i].parts);  // get the substitution
		if (sline != skel[i].unset)                         // and use if non-trivial
			s += sline;
		// if (sline != skel[i].unset && line["Organisation Name"]!="Aayatiin Foundation") dmessage("'" + sline + "'  != '" +  skel[i].unset + "'" + i); // debug
	}
	s += "</span></span>";
	return s; 
}

// match line with query
// query is an array of strings, all of which must match
function match(line, query) {
	for (var l in query) {
		var qf = query[l];
		if (qf.charAt(0) == "^") {
			// TODO: allow for errors, ? parse and create function when query first parsed
			// allow for blanks in ! part
			// note, clone of line so accidental use of "=" does not overwrite data
			if (!eval('r = Object.create(line); with (r) ' + qf.substring(1))) return false;
		} else if (qf.charAt(0) == "!") {
			// negative
			if (line.all.indexOf(qf.substring(1)) != -1) return false;
		} else {
			if (line.all.indexOf(qf) == -1) return false;
		}
	}
	return true;
}

// submit will submit the current query and process the results
function dbsubmit() {
 	// initialize the per query global variables
	var query = searchId.value.toLowerCase();
	var queryl = query.split(" ");
	
	var start = (new Date).getTime();
	
	var hits = 0;
	var s = "";
	for (var linex in addr) {
		var line = addr[linex];
		if (match(line, queryl)) {
			var ff = format(line);
			s += ff;
			hits++;
		}
	}
	var h = holderId;
	h.innerHTML = s;
	var diff = (new Date).getTime() - start;
	dmessage(", submit='" + query + "' time="+diff + " hits="+hits);
	if (hitsId != null) hitsId.innerHTML = hits;

}

var oldtime = 0;
// return time since last call
function time() {
	var now = (new Date).getTime();
	var diff = now - oldtime;
	oldtime = now;
	return diff;
}

// post a uri and return result string
function post(uri, msg) {
	time();
	var req;
	if (typeof XMLHttpRequest == 'function')
		req = new XMLHttpRequest();
	else
		req = new ActiveXObject("Microsoft.XMLHTTP");
	req.open("POST",uri,false);  // nb, POST ensures cache not used
	req.send("");
	var diff = time();
	dmessage(msg + "=" + diff);
	return req.responseText;
}


// output an error msg (should be optional on whether the field exists)
function message(msg) {
	if (messageId == null) { alert(msg); return; }  // don't output messages if not wanted
	messageId.innerText = msg;
	dmessage(msg);
	//var mmm = messageId.innerText + " " + msg;
	//if (mmm.length > 200) mmm = mmm.substring(mmm.length - 200);
	//messageId.innerText = mmm;
	
}

// output a debug msg (should be optional on whether the field exists)
function dmessage(msg) {
	if (dmessageId == null) return;  // don't output messages if not wanted
	var mmm = dmessageId.innerText + " " + msg;
	if (mmm.length > 200) mmm = mmm.substring(mmm.length - 200);
	dmessageId.innerText = mmm;	
}


// clear msg
function clearMessage() {
	if (messageId == null) return;  // don't output messages if not wanted
	messageId.innerText = "";
}

var fieldNames;  // array of field names

function mysorter(line) {
	return (line['last name'] + "\t" + line['first name']).toLowerCase();
}
function mysort(a, b) {
	var ma = mysorter(a);
	var mb = mysorter(b);
	return ma > mb ? 1 : ma < mb ? -1 : 0;
}

// resort, and reestablish xxid and currentXxid
function resort() {
	time();
	var obj = addr[currentXxid];
	addr.sort(mysort);
	for (var ln in addr) {
		if (ln != null) addr[ln].xxid = ln;
	}
	if (obj != null) currentXxid = obj.xxid;
	var sorttime = time();
	dmessage("sorttime=" + sorttime);

}

// read the tab separated file into an array of objects
// also compute and save .all for each line
function readfile() {
	// load a whole csv file, and then split it line by line
	var ssall = post("data"+fid, "readtime");
	var ss = ssall.replace(/\r/g,"").split("\n");
	var rr = [];
	fieldNames = ss[0].split("\t");
	for (var s in fieldNames) fieldNames[s] = fieldNames[s].trim();
	for (var line in ss) {
		if (line == 0) continue;
		var l = {};
		var ssl = ss[line].split("\t");
		if (ss[line] == "") continue;
		for (var k in ssl) {
			var f = ssl[k].trim();
			l[fieldNames[k]] = f;
		}
		makeall(l);
		rr[line-1] = l;
	}
	var parsetime = time();
	dmessage("parsetime="+parsetime);
	return rr;
}

// add appropriate .all field to l
function makeall(l) {
	var all = "$";
	for (var k in fieldNames) {
		var f = l[fieldNames[k]];
		if (f != "") all += fieldNames[k] + ":" + f + "$";
	}
	l.all = all.toLowerCase();
	l.xxid = null;
}

// query the time the file was last changed
function queryTime() {
	var resp = post("filetime.php?fid=" + fid, "querytime");
	return resp.trim();
}


// states and buttons

function updateReady() {
	if (updateId != null) updateId.disabled = false;
	if (insertId != null) insertId.disabled = true;
	if (deleteId != null) deleteId.disabled = true;
	if (clearId != null) clearId.disabled = false;
}
function deleteReady() {
	if (updateId != null) updateId.disabled = true;
	if (insertId != null) insertId.disabled = true;
	if (deleteId != null) deleteId.disabled = false;
	if (clearId != null) clearId.disabled = false;
}
function selectReady() {
	if (updateId != null) updateId.disabled = true;
	if (insertId != null) insertId.disabled = true;
	if (deleteId != null) deleteId.disabled = true;
	if (clearId != null) clearId.disabled = true;
}
function insertReady() {
	if (updateId != null) updateId.disabled = true;
	if (insertId != null) insertId.disabled = false;
	if (deleteId != null) deleteId.disabled = true;
	if (clearId != null) clearId.disabled = true;
}



// force update, and return the new object (or the old if no update was needed)
// Note that the new object may be both a different object, and have a different xxid
function update() {
	clearMessage();
	var oldVersionString = showObjectAsText(oldVersion());
	var newVersion = showFormAsObject();
	var newVersionString = showObjectAsText(newVersion);
	if (iupdate(oldVersionString, newVersionString, "UPDATE")) {
		addr.splice(oldVersion().xxid, 1, newVersion);
		// oldVersion() = newVersion;
		resort();
		dbsubmit();
		select(newVersion.xxid);  // reforce highlight, update form, etc
		return newVersion;
	} else {
		return oldVersion;
	}
	
}

// force delete
function dodelete() {
	if (currentXxid == null) { message("No selected object to delete"); return; }
	clearMessage();
	var oldVersionString = showObjectAsText(oldVersion());
	if (iupdate(oldVersionString + "\r\n", "", "DELETE")) {
		addr.splice(oldVersion().xxid, 1);
		resort();
		dbsubmit();
		currentXxid = null;
		doclear();
	}
	//deselect();
}

// force insert
function insert() {
	if (showFormAsObject().all == '$') { message("No data to insert"); return; }
	if (currentXxid != null) { message("Insert not valid as change"); return; }  // todo, make a 'replicate'
	clearMessage();
	var newVersion = showFormAsObject();
	var newVersionString = showObjectAsText(newVersion);
	if (iupdate("INSERT", newVersionString + "\r\n", "INSERT")) {
		addr.splice(0, 0, newVersion);
		resort();
		dbsubmit();
		select(newVersion.xxid);
	}
}

// clear the form
function doclear() {
	//oldVersion() = null;
	deselect();
}

// clean db
function cleandb() {	// TODO loop a few times
	iupdate("\t ", "\t", "REPLACEALL");
	iupdate(" \t", "\t", "REPLACEALL");
	iupdate("\r ", "\r", "REPLACEALL");
	iupdate(" \r", "\r", "REPLACEALL");
	iupdate("\n ", "\n", "REPLACEALL");
	iupdate(" \n", "\n", "REPLACEALL");
}

// force update, insert, delete: return true if local change needed to addr
// false return may be because update was impossible (in which case message is given), 
// or because another change on server means the whole has had to be refreshed
function iupdate(oldVersionString, 	newVersionString, op) {
	if (oldVersionString == newVersionString) {
		message("Update request: No update as no change made.");
		return false;
	} 
	
	var resp = post("addr.php?fid=" + fid + "&from=" + encodeURIComponent(oldVersionString) + "&to=" + encodeURIComponent(newVersionString) + "&op=" + op, "updatetime");
	var resps = resp.split("!");
	if ( resps[0] == "file updated and saved") {
		if (lastFiletime == resps[1]) {  // nobody else has been playing since we last updated
			lastFiletime = resps[2];
			message("OK, no full refresh needed");
			dmessage("filetime[" + resps[1] + "," + resps[2] + "] count=" + resps[3]);
			return true;
		} else {
			message("OK, but full refresh needed");
			dmessage("filetime[" + resps[1] + "," + resps[2] + "] count=" + resps[3]);
			refresh();
			return false;
		}
	} else {
		message("Update request failed: " + resp + " ... ");
		refresh();
		return false;
	}
}

// check if 'implicit' update is needed, eg after a form change that has not been confirmed
function updateCheck() {
	if (currentXxid == null) return;  // change not relevant
	// check for outstanding changes to old in form
	var oldVersionString = showObjectAsText(oldVersion());
	var newVersion = showFormAsObject();
	var newVersionString = showObjectAsText(newVersion);
	if (oldVersionString != newVersionString) {
		if (confirm("Do you want to commit the changes you have made to the form for the old selection?")) update();
	}
}

// deselect the current id (if any), give option to apply pending changes if any, and clear form
function deselect() {
	if (currentXxid == null) return;  // no change
	// remove highlight on old
	var old = document.getElementById('entry_' + currentXxid);
	if (old == null) alert("unexpected deselect with no element selected");
	if (old != null) old.className = "none";

	updateCheck();
	
	clearForm();
	currentXxid = null;
	selectReady();
}

// select the given id, delselect any old one
function select(xxid) {
	if (xxid == currentXxid) { 
		updateCheck();   	// no change
	} else {
		var obj = addr[xxid];
		// collect the obj, as any update may upset xxid
		// do work to ensure hightlight, etc.   
		deselect(currentXxid);  			// deselect old 
		
		updateForm(obj);				// make form match new
		currentXxid = obj.xxid;					// remember new
	}
	if (currentXxid != null) document.getElementById('entry_' + currentXxid).className = "currentClicked";  // highlight new
	deleteReady();
}

// this is called when an entry is clicked on the main view to show it in the form view
function entryClicked(xxid) {
	select(xxid);
}

var formsize;  // the number of fields in the file, and therefore in the form
// generate the form, using a line as example
function generateFormFromHeads() {
	var s = "<table>";
	formsize=0;
	for (var l in fieldNames) {
		s += '<tr align="right"><td>' + fieldNames[l] + '</td><td>';
		s += '<input type="text" class="formentry" id="form_' +formsize+ '" name="form_' +formsize+ '" ' +
				'onkeydown="formdown(\'' + l + '\', event)"  ' +
				'onkeypress="formpress(\'' + l + '\', event)"/>';
		s += '</td></tr>';
		formsize++;
	}
	s += "</table>";

	var f = formId;
	f.innerHTML += s;
	addr[null] = showFormAsObject();
}

// update the form from the given line
function updateForm(line) {
	var i=0;
	for (var l in line) {
		if (l != "all" && l != "format" && l != "xxid") {
			document.getElementById('form_' + i).value = line[l];
			i++;
		}
	}
}

// clear the form
function clearForm() {
	for (var i = 0; i < formsize; i++) document.getElementById('form_' + i).value = "";
	
}

function formdown(num, evt) {
	if (evt.keyCode == 27)   // esc
		document.getElementById('form_' + num).value = (currentXxid == null) ? "" : addr[currentXxid][fieldNames[num]];
	else if(currentXxid == null)
		insertReady();
	else
		updateReady();
}

function formpress(num, evt) {
	//alert("formpress: " + name + "  >" + evt);
}

// convert the form as text
// this is a (tab) separated line (field data only, no field names)
function showFormAsText(sep) {
	var s="";
	for (var i=0; i<formsize; i++)
		s += sep + document.getElementById('form_' + i).value
	return s.substring(1);
}

// convert the form as object
// this is a (tab) separated line (field data only, no field names)
function showFormAsObject(sep) {
	var s={};
	var all = "$";
	for (var i=0; i<formsize; i++) {
		s[fieldNames[i]] = document.getElementById('form_' + i).value;
		all += fieldNames[i] + ":" + s[fieldNames[i]];
	}
	makeall(s);	
	return s;
}

// line as text (format as in file)
// this is a (tab) separated line (field data only, no field names)
function showObjectAsText(line, sep) {
	if (sep === undefined) sep = "\t"
	var s="";
	for(var l in line)
		if (l != "all" && l != "format" && l != "xxid")
			s += sep + line[l];
	return s.substring(1);
}

var lastFiletime;
var refreshTimeout;
var refreshRate = 60000;  // refresh once a minute unless overridden ~ eg by refreshrate box

function refreshrateChange() {
	if (refreshrateId != null) refreshRate = parseInt(refreshrateId.value);
	clearTimeout(refreshTimeout);
	autoRefresh();
}

// function for start autorefresh based on time
function autoRefresh() {
	lastFiletime = queryTime();  dmessage(" initfiletime=["+ lastFiletime+"]");
	refreshTimeout = setTimeout("autoRefresh2()", refreshRate);
}

// repeated autorefresh attempts
function autoRefresh2() {
	var filetime = queryTime();  dmessage(".");  dmessage(" rfiletime=["+ filetime+"]");
	if (filetime != lastFiletime) {
		refresh();
		lastFiletime = filetime;
	}
	refreshTimeout = setTimeout("autoRefresh2()", refreshRate);
}

// todo, limit this
document.onkeydown = function(evt) {
	// http://www.cambiaresearch.com/articles/15/javascript-char-codes-key-codes
  	if (evt.target.className == "formentry") return;  // do not handle escape on single form entry
  evt = evt || window.event;
    if (evt.keyCode == 27) deselect();   // esc
	if (evt.keyCode == 46) dodelete();
};