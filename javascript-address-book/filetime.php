<?php
// query filetime for the $fid
$fid = "dataaddr.txt";

$ss = $_SERVER['QUERY_STRING'];
$fid = 'addr.txt';
parse_str($ss);

// fix $fid this AFTER the parse in case someone tries to corrupt another file
$fid = 'data'.$fid;

echo filemtime($fid);
?>		
