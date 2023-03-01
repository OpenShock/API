let utils = {
	isDarkMode() {
		const cook = utils.getCookie("settings_dark");
		if(cook !== "") {
			return cook === "true";
		}
		return false;
	},
	setDarkMode(dark) {
		utils.setCookie("settings_dark", dark, 3652);
	}
}

global.utils = utils;
