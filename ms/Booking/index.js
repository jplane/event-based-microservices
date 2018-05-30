
const express = require('express');
const mongoose = require('mongoose');
const uuid = require('uuidv4');
const axios = require('axios');
require('dotenv').config();

mongoose.connect(process.env.DB_CONNECTION);

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

app.post('/api/room/book', (req, res) => {
    Booking.create({
        id: uuid(),
        roomId: req.body.roomId,
        start: req.body.start,
        nights: req.body.nights,
        rate: {
            class: req.body.rateClass,
            price: req.body.price
        }
    }, async (err, b) => {
        if (err) {
            res.status(500).send(err)
        } else {
            const data = {
                id: b.roomId,
                available: 'false'
            };
            await axios.patch(process.env.AVAILABILITY_ENDPOINT_INTERNAL, data)
                       .then(() => axios.patch(process.env.AVAILABILITY_ENDPOINT_EXTERNAL, data))
                       .then(() => res.send(b.id))
                       .catch(e => res.status(500).send(e));
        }
    });
});

app.delete('/api/room/book/cancel/:id', (req, res) => {
    Booking.findOneAndDelete({
        id: req.params.id
    }, async (err, b) => {
        if (err) {
            res.status(500).send(err)
        } else {
            const data = {
                id: b.roomId,
                available: 'true'
            };
            await axios.patch(process.env.AVAILABILITY_ENDPOINT_INTERNAL, data)
                       .then(() => axios.patch(process.env.AVAILABILITY_ENDPOINT_EXTERNAL, data))
                       .then(() => res.send())
                       .catch(e => res.status(500).send(e));
        }
    });
});

app.listen(3000,
    () => console.log('App listening on port 3000'));
