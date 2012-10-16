<?php
// This is a php file to replace text in a file $fid 
// It is called with a GET or a POST, and the querystring from=....&to=....
// The file is read, modified, and written back.
//
// $fid		name of data file. This will be prepended with "data" to prevent change to non-data files
// $before  file contents before change
// $from    value to replace
// $to      value to replace with
// $count   number of replacements made
// $op      operation, INSERT, UPDATE, REPLACEALL
//
// When used by minidb.js, $from and $to represent complete tab separated lines.
//     For UPDATE, $from and $to have appropriate values.
//     For INSERT, $from is set to INSERT, with $to as new value
//     For DELETE, $from is the value to delete, with its trailing line end ("\r\n"), and $to is empty
$from = '?';
$to = '?';
$op = '?';
$ss = $_SERVER['QUERY_STRING'];
$fid = 'addr.txt';
parse_str($ss);

// fix $fid this AFTER the parse in case someone tries to corrupt another file
$fid = 'data'.$fid;
$logfid = $fid.'.log';
$logh = fopen($logfid, 'a');
date_default_timezone_set('Europe/London');
$date = date('Y m d H:i:s', time());


if (get_magic_quotes_gpc()) {
	$from = stripslashes($from);
	$to = stripslashes($to);
}

// from http://stackoverflow.com/questions/1252693/php-str-replace-that-only-acts-on-the-first-match
function str_replace_first($search, $replace, $subject) {
    return implode($replace, explode($search, $subject, 2));
}

$before = file_get_contents($fid);
$ok = false;
$count = "n/a"; 

if ($from == "?" || $to == "?" || $op == "?") {
	echo "bad from/to: from '$from' to '$to'";
} else {
	if ($op == 'INSERT') {
		if (strpos($before, $to) == false) {
			$after = $before.$to;
			$ok = true;
			fwrite($logh, $date . "\tins\t" . $to . "\n");
		} else {
			echo "Database already contains item to be inserted.";
		}
	} else if ($op == "UPDATE") {
		if (strpos($before, $from) == false) {
			echo "Database does not contain item to be replaced.";
		} else {
			$after = str_replace_first($from, $to, $before);
			fwrite($logh, $date . "\tupold\t" . $from . "\n");
			fwrite($logh, $date . "\tupnew\t" . $to . "\n");
			$ok = true;
		}
	} else if ($op == "DELETE") {
		if (strpos($before, $from) == false) {
			echo "Database does not contain item to be replaced.";
		} else {
			$after = str_replace_first($from, $to, $before);
			fwrite($logh, $date . "\tdel\t" . $from . "\n");
			$ok = true;
		}
	} else if ($op == "REPLACEALL") {
		$count = 0;
		$nc = 999;
		$after = $before;
		while ($nc != 0) {
			$after = str_replace($from, $to, $after, $nc);
			$ok = true;
			$count = $count + $nc;
		}
	} else {
		echo "Unknown op " + $op;
	}
	
	if ($ok) {
		$ftpre = filemtime($fid);
		file_put_contents($fid, $after, LOCK_EX);
		$post = filemtime("./$fid");  // use a different way of referencing the file otherwise we get an old cached value
		echo "file updated and saved!$ftpre!$post!$count!" ;
	} else {
		echo "... file not saved.";
	}
}
fclose($logh);
?>		
