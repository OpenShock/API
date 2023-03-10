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
	setLogin() {
		this.setCookie("loggedIn", "true", 7);
	},
	authExists() {
		let user = this.getCookie("loggedIn");
		return user === "true";
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
	},
	getError(err) {
		if(err !== undefined && err.response !== undefined) {
			if(err.response.data !== undefined && err.response.data.message !== undefined)
				return `${err.response.status} with ${err.response.data.message}`;

			return `${err.response.status} ${err.response.statusText}`;
		}
		return "Something went terribly wrong, no further info."
	}
}

global.utils = utils;
