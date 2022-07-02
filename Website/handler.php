<?php
	$LogFile = 'data/logs.txt';
	if(!empty($_POST)) {
		$Config = json_decode(file_get_contents('data/config.json'), true);
		$ServerLoaderVersion = filter_var($_POST['LoaderVersion'], FILTER_VALIDATE_REGEXP, array('options' => array('regexp'=>'/^[0-9]{1,3}.[0-9]{1,3}.[0-9]{1,3}.[0-9]{1,3}$/')));
		$ServerIP = $_SERVER['REMOTE_ADDR'];
		$ServerPort = filter_var($_POST['ServerPort'], FILTER_VALIDATE_REGEXP, array('options' => array('regexp'=>'/^[0-9]{1,5}$/')));
		$License = filter_var($_POST['License'], FILTER_VALIDATE_REGEXP, array('options' => array('regexp'=>'/^[a-zA-Z0-9]{4}-[a-zA-Z0-9]{4}-[a-zA-Z0-9]{4}-[a-zA-Z0-9]{4}$/')));
		$Response = array('ValidLoaderVersion' => false, 'ValidLicense' => false, 'ValidAddress' => false,'UserPluginsNames' => '', 'UserPlugins' => '');
		if (!empty($ServerLoaderVersion) && !empty($ServerPort) && !empty($License)) {
			if ($ServerLoaderVersion == $Config["LoaderVersion"]) {
				$Response['ValidLoaderVersion'] = true;
				$Users = json_decode(file_get_contents('data/users.json'), true);
				$FirstPluginAdded = false;
				foreach ($Users['Users'] as $User) {
					if ($User['License'] == $License) {
						$Response['ValidLicense'] = true;
						if ($User['AddressValidation']) {
							if (($User['ServerIP'] == $ServerIP) && ($User['ServerPort'] == $ServerPort)) {
								$Response['ValidAddress'] = true;
							}
						} else $Response['ValidAddress'] = true;
						if ($Response['ValidAddress']) {
							$Plugins = json_decode(file_get_contents('data/plugins.json'), true);
							foreach (array_diff($User['Plugins'], $User['DisabledPlugins']) as $ID) {
								foreach ($Plugins['Plugins'] as $Plugin) {
									if ($Plugin['ID'] == $ID) {
										if ($FirstPluginAdded) {
											$Plugin['Base64'] = base64_encode(file_get_contents('plugins/'.$Plugin['Name'].'.dll'));
											$Response['UserPluginsNames'] .= ','.$Plugin['Name'];
											$Response['UserPlugins'] .= ','.$Plugin['Base64'];
										} else {
											$Plugin['Base64'] = base64_encode(file_get_contents('plugins/'.$Plugin['Name'].'.dll'));
											$Response['UserPluginsNames'] .= $Plugin['Name'];
											$Response['UserPlugins'] .= $Plugin['Base64'];
											$FirstPluginAdded = true;
										}
									}
								}
							}
						}
						break;
					}
				}
			}
		}
		$LogString = "============== POST DATA ==============\n";
		$LogString .= "Loader Version: ".$_POST["LoaderVersion"]."\n";
		$LogString .= "Server IP: ".$_POST["ServerIP"]."\n";
		$LogString .= "Server Port: ".$_POST["ServerPort"]."\n";
		$LogString .= "License: ".$_POST["License"]."\n";
		$LogString .= "=======================================\n\n";
		file_put_contents($LogFile, $LogString, FILE_APPEND | LOCK_EX);
		echo json_encode($Response);
	} else {
		file_put_contents($LogFile, "UnknownRequest\n\n", FILE_APPEND | LOCK_EX);
	}
?>
