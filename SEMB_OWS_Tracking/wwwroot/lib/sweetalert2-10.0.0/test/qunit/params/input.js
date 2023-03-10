const { $, Swal, SwalWithoutAnimation, isVisible, TIMEOUT, triggerKeydownEvent, dispatchCustomEvent, isIE } = require('../helpers')
const sinon = require('sinon/pkg/sinon')

QUnit.test('should throw console error about unexpected input type', (assert) => {
  const _consoleError = console.error
  const spy = sinon.spy(console, 'error')
  Swal.fire({ input: 'invalid-input-type' })
  console.error = _consoleError
  assert.ok(spy.calledWith('SweetAlert2: Unexpected type of input! Expected "text", "email", "password", "number", "tel", "select", "radio", "checkbox", "textarea", "file" or "url", got "invalid-input-type"'))
})

QUnit.test('input text', (assert) => {
  const done = assert.async()

  const string = 'Live for yourself'
  Swal.fire({
    input: 'text',
  }).then((result) => {
    assert.equal(result.value, string)
    done()
  })

  Swal.getInput().value = string
  Swal.clickConfirm()
})

QUnit.test('input textarea', (assert) => {
  const done = assert.async()

  Swal.fire({
    input: 'textarea',
    inputAutoTrim: false
  }).then((result) => {
    assert.equal(result.value, 'hola!')
    done()
  })

  // Enter should not submit but put a newline to the textarea
  triggerKeydownEvent(Swal.getInput(), 'Enter')

  Swal.getInput().value = 'hola!'
  Swal.clickConfirm()
})

QUnit.test('input email + built-in email validation', (assert) => {
  const done = assert.async()

  const invalidEmailAddress = 'blah-blah@zzz'
  const validEmailAddress = 'team+support+a.b@example.com'
  SwalWithoutAnimation.fire({ input: 'email' }).then((result) => {
    assert.equal(result.value, validEmailAddress)
    done()
  })

  Swal.getInput().value = invalidEmailAddress
  Swal.clickConfirm()
  setTimeout(() => {
    assert.ok(isVisible(Swal.getValidationMessage()))
    assert.ok(Swal.getValidationMessage().textContent.indexOf('Invalid email address') !== -1)

    Swal.getInput().value = validEmailAddress
    triggerKeydownEvent(Swal.getInput(), 'Enter')
  }, TIMEOUT)
})

QUnit.test('input url + built-in url validation', (assert) => {
  const done = assert.async()

  const invalidUrl = 'sweetalert2.github.io'
  const validUrl = 'https://sweetalert2.github.io/'
  SwalWithoutAnimation.fire({ input: 'url' }).then((result) => {
    assert.equal(result.value, validUrl)
    done()
  })

  Swal.getInput().value = invalidUrl
  Swal.clickConfirm()
  setTimeout(() => {
    assert.ok(isVisible(Swal.getValidationMessage()))
    assert.ok(Swal.getValidationMessage().textContent.indexOf('Invalid URL') !== -1)

    Swal.getInput().value = validUrl
    triggerKeydownEvent(Swal.getInput(), 'Enter')
  }, TIMEOUT)
})

QUnit.test('input select', (assert) => {
  const done = assert.async()

  const selected = 'dos'
  Swal.fire({
    input: 'select',
    inputOptions: { uno: 1, dos: 2 },
    inputPlaceholder: 'Choose a number'
  }).then((result) => {
    assert.equal(result.value, selected)
    done()
  })

  assert.equal(Swal.getInput().value, '')

  const placeholderOption = Swal.getInput().querySelector('option')
  assert.ok(placeholderOption.disabled)
  assert.ok(placeholderOption.selected)
  assert.equal(placeholderOption.textContent, 'Choose a number')

  Swal.getInput().value = selected
  Swal.clickConfirm()
})

QUnit.test('input select with optgroup and root options', (assert) => {
  const done = assert.async()

  const selected = 'tr??s ponto um'
  Swal.fire({
    input: 'select',
    inputOptions: { 'um': 1.0, 'dois': 2.0, 'tr??s': { 'tr??s ponto um': 3.1, 'tr??s ponto dois': 3.2 } },
    inputPlaceholder: 'Choose an item'
  }).then((result) => {
    assert.equal(result.value, selected)
    done()
  })
  assert.equal(Swal.getInput().value, '')
  const placeholderOption = Swal.getInput().querySelector('option')
  assert.ok(placeholderOption.disabled)
  assert.ok(placeholderOption.selected)
  assert.equal(placeholderOption.textContent, 'Choose an item')
  Swal.getInput().value = selected
  Swal.clickConfirm()
})

QUnit.test('input select with only optgroups options', (assert) => {
  const done = assert.async()

  const selected = 'tr??s ponto dois'
  Swal.fire({
    input: 'select',
    inputOptions: { 'dois': { 'dois ponto um': 2.1, 'dois ponto dois': 2.2 }, 'tr??s': { 'tr??s ponto um': 3.1, 'tr??s ponto dois': 3.2 } },
    inputPlaceholder: 'Choose an item'
  }).then((result) => {
    assert.equal(result.value, selected)
    done()
  })
  assert.equal(Swal.getInput().value, '')
  const placeholderOption = Swal.getInput().querySelector('option')
  assert.ok(placeholderOption.disabled)
  assert.ok(placeholderOption.selected)
  assert.equal(placeholderOption.textContent, 'Choose an item')
  Swal.getInput().value = selected
  Swal.clickConfirm()
})

QUnit.test('input select with inputOptions as Promise', (assert) => {
  const done = assert.async()

  Swal.fire({
    input: 'select',
    inputOptions: Promise.resolve({ one: 1, two: 2 }),
    onOpen: () => {
      setTimeout(() => {
        Swal.getInput().value = 'one'
        assert.equal(Swal.getInput().value, 'one')
        done()
      }, TIMEOUT)
    }
  })
})

QUnit.test('input select with inputOptions as object containing toPromise', (assert) => {
  const done = assert.async()

  Swal.fire({
    input: 'select',
    inputOptions: {
      toPromise: () => Promise.resolve({ three: 3, four: 4 })
    },
    onOpen: () => {
      setTimeout(() => {
        Swal.getInput().value = 'three'
        assert.equal(Swal.getInput().value, 'three')
        done()
      }, TIMEOUT)
    }
  })
})

QUnit.test('input text w/ inputPlaceholder as configuration', (assert) => {
  const done = assert.async()

  Swal.fire({
    input: 'text',
    inputPlaceholder: 'placeholder text'
  })

  assert.equal(Swal.getInput().value, '')
  assert.equal(Swal.getInput().placeholder, 'placeholder text')

  done()
})

QUnit.test('input checkbox', (assert) => {
  const done = assert.async()

  Swal.fire({ input: 'checkbox', inputAttributes: { name: 'test-checkbox' } }).then((result) => {
    assert.equal(checkbox.getAttribute('name'), 'test-checkbox')
    assert.equal(result.value, '1')
    done()
  })

  const checkbox = $('.swal2-checkbox input')
  checkbox.checked = true
  Swal.clickConfirm()
})

QUnit.test('input range', (assert) => {
  Swal.fire({ input: 'range', inputAttributes: { min: 1, max: 10 }, inputValue: 5 })
  const input = Swal.getInput()
  const output = $('.swal2-range output')
  assert.equal(input.getAttribute('min'), '1')
  assert.equal(input.getAttribute('max'), '10')
  assert.equal(input.value, '5')

  if (!isIE) { // TODO (@limonte): make IE happy
    input.value = 10
    dispatchCustomEvent(input, 'input')
    assert.equal(output.textContent, '10')

    input.value = 9
    dispatchCustomEvent(input, 'change')
    assert.equal(output.textContent, '9')
  }
})

QUnit.test('input type "select", inputOptions Map', (assert) => {
  const inputOptions = new Map()
  inputOptions.set(2, 'Richard Stallman')
  inputOptions.set(1, 'Linus Torvalds')
  SwalWithoutAnimation.fire({
    input: 'select',
    inputOptions,
    inputValue: 1
  })
  assert.equal($('.swal2-select').querySelectorAll('option').length, 2)
  assert.equal($('.swal2-select option:nth-child(1)').innerHTML, 'Richard Stallman')
  assert.equal($('.swal2-select option:nth-child(1)').value, '2')
  assert.equal($('.swal2-select option:nth-child(2)').innerHTML, 'Linus Torvalds')
  assert.equal($('.swal2-select option:nth-child(2)').value, '1')
  assert.equal($('.swal2-select option:nth-child(2)').selected, true)
})

QUnit.test('input type "select", inputOptions Map with optgroup and root options', (assert) => {
  const inputOptions = new Map()
  inputOptions.set(2, 'Richard Stallman')
  inputOptions.set(1, 'Linus Torvalds')

  const optGroup1Options = new Map()
  optGroup1Options.set(100, 'jQuery')
  optGroup1Options.set(200, 'ReactJS')
  optGroup1Options.set(300, 'VueJS')
  inputOptions.set('Frameworks optgroup', optGroup1Options)

  SwalWithoutAnimation.fire({
    input: 'select',
    inputOptions,
    inputValue: 1
  })
  assert.equal($('.swal2-select').querySelectorAll('option').length, 5)
  assert.equal($('.swal2-select').querySelectorAll('optgroup').length, 1)
  assert.equal($('.swal2-select option:nth-child(1)').innerHTML, 'Richard Stallman')
  assert.equal($('.swal2-select option:nth-child(1)').value, '2')
  assert.equal($('.swal2-select option:nth-child(2)').innerHTML, 'Linus Torvalds')
  assert.equal($('.swal2-select option:nth-child(2)').value, '1')
  assert.equal($('.swal2-select option:nth-child(2)').selected, true)
  assert.equal($('.swal2-select optgroup option:nth-child(1)').innerHTML, 'jQuery')
  assert.equal($('.swal2-select optgroup option:nth-child(1)').value, '100')
  assert.equal($('.swal2-select optgroup option:nth-child(2)').innerHTML, 'ReactJS')
  assert.equal($('.swal2-select optgroup option:nth-child(2)').value, '200')
  assert.equal($('.swal2-select optgroup option:nth-child(3)').innerHTML, 'VueJS')
  assert.equal($('.swal2-select optgroup option:nth-child(3)').value, '300')
})

QUnit.test('input type "select", inputOptions Map with only optgroup options', (assert) => {
  const inputOptions = new Map()

  const frameworkOptGroupOptions = new Map()
  frameworkOptGroupOptions.set('100', 'jQuery')
  frameworkOptGroupOptions.set('200', 'ReactJS')
  frameworkOptGroupOptions.set('300', 'VueJS')
  inputOptions.set('Frameworks optgroup', frameworkOptGroupOptions)

  const libOptGroupOptions = new Map()
  libOptGroupOptions.set('1000', 'SweetAlert2')
  libOptGroupOptions.set('2000', 'Bootstrap4')
  inputOptions.set('Library optgroup', libOptGroupOptions)

  SwalWithoutAnimation.fire({
    input: 'select',
    inputOptions,
    inputValue: '1000'
  })
  assert.equal($('.swal2-select').querySelectorAll('option').length, 5)
  assert.equal($('.swal2-select').querySelectorAll('optgroup').length, 2)
  assert.equal($('.swal2-select optgroup:nth-child(1) option:nth-child(1)').innerHTML, 'jQuery')
  assert.equal($('.swal2-select optgroup:nth-child(1) option:nth-child(1)').value, '100')
  assert.equal($('.swal2-select optgroup:nth-child(1) option:nth-child(2)').innerHTML, 'ReactJS')
  assert.equal($('.swal2-select optgroup:nth-child(1) option:nth-child(2)').value, '200')
  assert.equal($('.swal2-select optgroup:nth-child(1) option:nth-child(3)').innerHTML, 'VueJS')
  assert.equal($('.swal2-select optgroup:nth-child(1) option:nth-child(3)').value, '300')
  assert.equal($('.swal2-select optgroup:nth-child(2) option:nth-child(1)').innerHTML, 'SweetAlert2')
  assert.equal($('.swal2-select optgroup:nth-child(2) option:nth-child(1)').value, '1000')
  assert.equal($('.swal2-select optgroup:nth-child(2) option:nth-child(1)').selected, true)
  assert.equal($('.swal2-select optgroup:nth-child(2) option:nth-child(2)').innerHTML, 'Bootstrap4')
  assert.equal($('.swal2-select optgroup:nth-child(2) option:nth-child(2)').value, '2000')
})

QUnit.test('input type "radio", inputOptions Map', (assert) => {
  const inputOptions = new Map()
  inputOptions.set(2, 'Richard Stallman')
  inputOptions.set(1, 'Linus Torvalds')
  Swal.fire({
    input: 'radio',
    inputOptions,
    inputValue: 1
  })
  assert.equal($('.swal2-radio').querySelectorAll('label').length, 2)
  assert.equal($('.swal2-radio label:nth-child(1)').textContent, 'Richard Stallman')
  assert.equal($('.swal2-radio label:nth-child(1) input').value, '2')
  assert.equal($('.swal2-radio label:nth-child(2)').textContent, 'Linus Torvalds')
  assert.equal($('.swal2-radio label:nth-child(2) input').value, '1')
  assert.equal($('.swal2-radio label:nth-child(2) input').checked, true)
})

QUnit.test('input radio', (assert) => {
  Swal.fire({
    input: 'radio',
    inputOptions: {
      one: 'one',
      two: 'two'
    }
  })

  assert.equal($('.swal2-radio').querySelectorAll('label').length, 2)
  assert.equal($('.swal2-radio').querySelectorAll('input[type="radio"]').length, 2)
})

QUnit.test('Swal.getInput() should be available in .then()', (assert) => {
  const done = assert.async()

  SwalWithoutAnimation.fire({
    input: 'text',
  }).then(() => {
    assert.ok(Swal.getInput())
    done()
  })
  Swal.close()
})

QUnit.test('Swal.getInput() should return null when a popup is disposed', (assert) => {
  const done = assert.async()

  SwalWithoutAnimation.fire({
    input: 'text',
    onAfterClose: () => {
      setTimeout(() => {
        assert.notOk(Swal.getInput())
        done()
      }, TIMEOUT)
    }
  })
  Swal.close()
})

QUnit.test('popup should expand and shrink accordingly to textarea width', (assert) => {
  const done = assert.async()
  SwalWithoutAnimation.fire({
    input: 'textarea',
  })
  Swal.getInput().style.width = '600px'
  setTimeout(() => {
    assert.equal(Swal.getPopup().style.width, '640px')

    Swal.getInput().style.width = '100px'
    setTimeout(() => {
      assert.equal(Swal.getPopup().style.width, '')
      done()
    })
  })
})
