class SanityChecks {

	checkUsername(username) {
		let arr = [];

		if (username.length <= 3) arr.push("Minimum length of 3 characters");
		if (username.length >= 32) arr.push("Maximum length of 32 characters");
		if (!username.match(/^[a-zA-Z0-9/.!'_-]*$/)) arr.push("Illegal character used");

		return arr;
	}

	checkEmail(email) {
		let arr = [];

		if (email.length <= 3) arr.push("Minimum length of 3 characters");
		if (email.length >= 50) arr.push("Maximum length of 50 characters");
		if (!email.toLowerCase().match(/^(([^<>()[\]\\.,;:\s@"]+(\.[^<>()[\]\\.,;:\s@"]+)*)|(".+"))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$/)) arr.push("Email is not valid")

		return arr;
	}

	checkPassword(password) {
		let arr = [];

		if (password.length <= 8) arr.push("Minimum length of 8 characters");
		if (password.length >= 128) arr.push("Maximum length of 128 characters");
		if (!password.match(/^[a-zA-Z0-9@#$%/.!'_-]*$/)) arr.push("Illegal character used");
		if(!this.hasLowerCase(password)) arr.push("One lowercase character");
		if(!this.hasUpperCase(password)) arr.push("One uppercase character");
		if(!this.hasNumber(password)) arr.push("One number");
		if(!this.hasSpecial(password)) arr.push("One special character");

		return arr;
	}

	hasLowerCase(str) {
		return (/[a-z]/.test(str));
	}

	hasUpperCase(str) {
		return (/[A-Z]/.test(str));
	}

	hasNumber(str) {
		return (/[0-9]/.test(str));
	}

	hasSpecial(str) {
		return (/[@#$%/.!'_-]/.test(str));
	}
}

export default new SanityChecks();
