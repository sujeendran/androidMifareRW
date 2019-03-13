# androidMifareRW
Android Xamarin Mifare Reader and Writer App

This is a sample application made for Reading and Writing Mifare Tags with your Android phone. It's a modified version of the https://github.com/xamarin/monodroid-samples/tree/master/NfcSample example.

It was tested with Android Oreo 8.1 with API 27 and worked without any issue. Do note that there is no prompts for enabling NFC or WiFi(for getting SSID information example function), so make sure to enable them manually while testing.

The read and write is performed only on Sector 1 and avoids the trailing block to avoid sector corruption.
