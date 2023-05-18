require("@/globals/config/config." + process.env.NODE_ENV + ".js");
require("@/js/utils.js");
import "bootstrap/scss/bootstrap.scss";
import ApiCall from '/src/js/ApiCall';
import toastr from 'toastr';
toastr.options = {
	"positionClass": "toast-bottom-right"
}
global.toastr = toastr;
global.apiCall = ApiCall;
global.COMMIT_HASH = process.env.COMMIT_HASH;

import jQuery from 'jquery';
global.jQuery = jQuery;
global.$ = jQuery;
import {createApp} from 'vue';

import 'bootstrap/dist/css/bootstrap.css'
import 'bootstrap-vue-next/dist/bootstrap-vue-next.css'
import 'normalize.css';
import 'toastr/build/toastr.css';
import "@fontsource/poppins";
import "./App.scss";
import 'sweetalert2/dist/sweetalert2.min.css';

import App from '@/App';
import Footer from '@/Footer';
import router from '@/router';
import store from '@/store';
import mitt from 'mitt';
import BootstrapVueNext from 'bootstrap-vue-next'
import VueSweetalert2 from 'vue-sweetalert2';

import '@imengyu/vue3-context-menu/lib/vue3-context-menu.css'
import ContextMenu from '@imengyu/vue3-context-menu'

import { library, dom } from '@fortawesome/fontawesome-svg-core';
import { fas } from '@fortawesome/free-solid-svg-icons';
import { far } from '@fortawesome/free-regular-svg-icons';
import { fab } from '@fortawesome/free-brands-svg-icons';
library.add(fas, far, fab);
dom.watch();

const emitter = mitt();

const app = createApp(App)
	.use(router)
	.use(store)
	.use(BootstrapVueNext)
	.use(VueSweetalert2)
	.use(ContextMenu);

app.config.globalProperties.emitter = emitter;
app.config.devtools = true;
global.emitter = emitter;

app.mount('#app');

const footerApp = createApp(Footer);
footerApp.mount('#footerApp');