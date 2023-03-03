let utils = {
	getCookie(cname) {
		let name = cname + "=";
		let ca = document.cookie.split(';');
		for(let i = 0; i < ca.length; i++) {
			let c = ca[i];
			while (c.charAt(0) == ' ') {
				c = c.substring(1);
			}
			if (c.indexOf(name) == 0) {
				return c.substring(name.length, c.length);
			}
		}
		return "";
	},
	setLogin(authkey) {
		this.setCookie("shockLinkSession", authkey, 30);
	},
	authExists() {
		let user = this.getCookie("shockLinkSession");
		return user !== "";
	},
	getAuthKey() {
		return this.getCookie("shockLinkSession");
	},
	isDarkMode() {
		const cook = this.getCookie("settings_dark");
		if(cook !== "") {
			return cook === "true";
		}
		return false;
	},
	setDarkMode(dark) {
		this.setCookie("settings_dark", dark, 3652);
	},
	setCookie(cname, cvalue, exdays) {
		const d = new Date();
		d.setTime(d.getTime() + (exdays * 24 * 60 * 60 * 1000));
		let expires = "expires="+d.toUTCString();
		document.cookie = cname + "=" + cvalue + ";" + expires + ";path=/";
	}
}

global.utils = utils;