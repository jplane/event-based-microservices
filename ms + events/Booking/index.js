
const express = require('express');
const mongoose = require('mongoose');
const uuid = require('uuidv4');
const axios = require('axios');
require('dotenv').config();

mongoose.connect(process.env.DB_CONNECTION, {
    auth: {
      user: process.env.DB_USER,
      password: process.env.DB_PWD
    }
  });

const bookingSchema = new mongoose.Schema({
    id: 'string',
    roomId: 'number',
    start: 'date',
    nights: 'number',
    rate: {
        class: 'string',
        price: 'number'
    }
});

const Booking = mongoose.model('Booking', bookingSchema);

const app = express();

app.use(express.json());

app.post('/booking/create', (req, res) => {
    Booking.create({
        id: uuid(),
        roomId: req.body.roomId,
        start: new Date(req.body.start),
        nights: req.body.nights,
        rate: req.body.rate
    }, async (err, b) => {
        if (err) {
            res.status(500).send(err)
        } else {
            const evts = [{
                id: uuid(),
                eventType: 'bookingCreated',
                subject: b.id,
                eventTime: new Date(),
                data: {
                    booking: b
                }
            }];
            const opts = {
                headers: {
                    'aeg-sas-key': process.env.AEG_SAS_KEY
                }
            };
            try {
                await axios.post(process.env.BOOKING_TOPIC_ENDPOINT, evts, opts)
                res.send(b.id);
            } catch (e) {
                res.status(500).send(e.message);
            }
        }
    });
});

app.delete('/booking/cancel/:id', (req, res) => {
    Booking.findOneAndDelete({
        id: req.params.id
    }, async (err, b) => {
        if (err) {
            res.status(500).send(err)
        } else {
            const evts = [{
                id: uuid(),
                eventType: 'bookingCanceled',
                subject: b.id,
                eventTime: new Date(),
                data: {
                    booking: b
                }
            }];
            const opts = {
                headers: {
                    'aeg-sas-key': process.env.AEG_SAS_KEY
                }
            };
            try {
                await axios.post(process.env.BOOKING_TOPIC_ENDPOINT, evts, opts)
                res.send(b.id);
            } catch (e) {
                res.status(500).send(e.message);
            }
        }
    });
});

app.listen(3000,
    () => console.log('App listening on port 3000'));
