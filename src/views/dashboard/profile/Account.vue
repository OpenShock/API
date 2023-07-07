<template>
  <b-container class="row-spacer">
    <b-row class="base-widget">
      <b-container>
        <h4>General Account Information</h4>
        <b-row class="my-1">
          <b-col sm="2">
            <label for="id">User ID:</label>
          </b-col>
          <b-col sm="10">
            <b-form-input id="id" plaintext v-model="user.id"></b-form-input>
          </b-col>
        </b-row>

        <b-row class="my-1">
          <b-col sm="2">
            <label for="username">Username:</label>
          </b-col>
          <b-col sm="10">
            <b-form-input id="username" plaintext v-model="user.name"></b-form-input>
          </b-col>
        </b-row>

        <b-row class="my-1">
          <b-col sm="2">
            <label for="email">Email:</label>
          </b-col>
          <b-col sm="10">
            <b-form-input id="email" plaintext v-model="user.email"></b-form-input>
          </b-col>
        </b-row>
      </b-container>
    </b-row>
    <b-row class="base-widget">
      <b-form-group id="formFile" content-cols-lg="7" style="margin: 0" label="Upload a new profile picture"
        label-for="input-horizontal">

        <div class="mb-3">
          <input @change="fileChanged" class="form-control"
            accept="image/png, image/gif, image/jpeg, image/webp, image/svg+xml" type="file" id="formFile">
        </div>
      </b-form-group>
    </b-row>

    <b-modal style="--bs-modal-width: 1000px;" class="crop-modal" v-model="modal.image.open"
      title="Upload new profile picture" ok-title="Upload" @ok.prevent="cropImage"
      :okDisabled="modal.image.uploading" :cancelDisabled="modal.image.uploading">
      <b-container style="padding: 0;">
        <b-row v-if="!modal.image.uploading">
          <vue-cropper align-v="center" class="cropper" :movable="false" ref="cropper" :src="user.image"
            :aspect-ratio="1 / 1" alt="Profile Picture">
          </vue-cropper>
        </b-row>
        <b-row v-else style="text-align: center;">
          <loading-with-text>Uploading image...<br> Progress: {{ modal.image.progress }}</loading-with-text>
          <b-progress class="progress-img" :value="modal.image.progress" :max="100" show-progress animated></b-progress>
          <p v-if="modal.image.progress > 99">Server processing...</p>
          <p v-else>Uploading data...</p>
        </b-row>
      </b-container>
    </b-modal>
  </b-container>
</template>

<script>
import axios from 'axios';
import VueCropper from 'vue-cropperjs';
import 'cropperjs/dist/cropper.css';
import LoadingWithText from '../../utils/LoadingWithText.vue';

export default {
  components: { VueCropper, LoadingWithText },
  data() {
    return {
      modal: {
        image: {
          open: false,
          uploading: false,
          progress: 0
        }
      }
    }
  },
  methods: {
    dataURLtoFile(dataurl, filename) {
      const arr = dataurl.split(',')
      const mime = arr[0].match(/:(.*?);/)[1]
      const bstr = atob(arr[1])
      let n = bstr.length
      const u8arr = new Uint8Array(n)
      while (n) {
        u8arr[n - 1] = bstr.charCodeAt(n - 1)
        n -= 1 // to make eslint happy
      }
      return new File([u8arr], filename, { type: mime })
    },
    async cropImage() {
      const cropImg = this.$refs.cropper.getCroppedCanvas().toDataURL();

      this.modal.image.uploading = true;
      // generate file from base64 string
      const file = this.dataURLtoFile(cropImg, "yes");
      // put file into form data
      const data = new FormData();
      data.append('avatar', file, file.name);

      try {
        await axios({
          method: "POST",
          url: config.apiUrl + '1/users/self/avatar',
          data: data,
          headers: {
            'Content-Type': 'multipart/form-data'
          },
          onUploadProgress: event => this.modal.image.progress = (event.loaded / event.total) * 100
        });
      } catch (err) {
        console.log(err);
        toastr.error(utils.getError(err), "API interaction failed");
        if(err.response !== undefined && err.response.status === 401) {
          router.push('/account/login');
          utils.setLogin("");
          return;
        }

        this.modal.image.uploading = false;
        this.modal.image.open = false;
        this.$swal('Error!', 'Error while uploading profile picture!', 'error');
        return;
      }

      this.modal.image.uploading = false;
      this.$store.dispatch('getSelf');
      this.modal.image.open = false;

      this.$swal('Success!', 'Successfully updated profile picture!', 'success');

    },
    fileChanged(e) {
      const file = e.target.files[0];

      if (typeof FileReader === 'function') {
        const reader = new FileReader();
        reader.onload = (event) => {
          this.imgSrc = event.target.result;
          // rebuild cropperjs with the updated source
          this.$refs.cropper.replace(event.target.result);

          this.modal.image.open = true;
        };
        reader.readAsDataURL(file);
      } else {
        alert('Sorry, FileReader API not supported');
      }
    }
  },
  computed: {
    user() {
      return this.$store.state.user;
    }
  }
}
</script>

<style lang="scss">
.cropper {
  max-width: calc(100% - 24px);
  max-height: 80vh;
}

.progress-img {
  padding: 0;
  margin-left: 12px;
  margin-right: 12px;
  width: calc(100% - 24px);
}

.my-1 {
  .col-sm-2 {
    align-self: center;
  }
}
</style>
