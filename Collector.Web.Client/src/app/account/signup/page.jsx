import React, { useState, useEffect } from 'react';
import './page.css';

//components
import Container from '@/components/forms/container';
import Input from '@/components/forms/input';
import messages from '@/helpers/messages';
//api
import { useSession } from '@/context/session';
import { Users } from '@/api/account/users';

/**
 * <summary>Sign Up Page</summary>
 * <description>Allows new users to register an account, handling invite tokens and form validation.</description>
 */
export default function SignUp() {
  //context
  const session = useSession();

  //state
  const [formData, setFormData] = useState({
    firstName: '',
    lastName: '',
    email: '',
    password: '',
    confirmPassword: ''
  });
  const [errors, setErrors] = useState({});
  const [formState, setFormState] = useState('new');
  const emailRegex = /^[a-zA-Z0-9._+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,10}$/;

  //api
  const { addUser } = Users(session);

  //actions
  const handleChange = (e) => {
    e.preventDefault();
    const { name, value } = e.target;
    const data = { ...formData, [name]: value };

    const errs = errors;
    let changed = false;
    if (errs.firstName && data.firstName != '') {
      errs.firstName = null;
      changed = true;
    }
    if (errs.lastName && data.lastName != '') {
      errs.lastName = null;
      changed = true;
    }
    if (errs.email && data.email != '') {
      errs.email = null;
      changed = true;
    }
    if (errs.password && data.password != '') {
      errs.password = null;
      changed = true;
    }
    if (errs.confirmPassword && data.password == data.confirmPassword) {
      errs.confirmPassword = null;
      changed = true;
    }
    if (changed == true) {
      setErrors(errs);
    }
    setFormData(data);
  };

  const handleSubmit = (e) => {
    e.preventDefault();
    const newErrors = { exists: false };
    //validate form
    if (formData.firstName == '') {
      newErrors.firstName = 'required';
      newErrors.exists = true;
    }
    if (formData.lastName == '') {
      newErrors.lastName = 'required';
      newErrors.exists = true;
    }
    if (formData.email == '') {
      newErrors.email = 'required';
      newErrors.exists = true;
    }
    if (!emailRegex.test(formData.email)) {
      newErrors.email = 'invalid email';
      newErrors.exists = true;
    }
    if (formData.password == '') {
      newErrors.password = 'required';
      newErrors.exists = true;
    }
    if (formData.password != formData.confirmPassword) {
      newErrors.confirmPassword = 'does not match';
      newErrors.exists = true;
    }
    setErrors(newErrors);

    if (!newErrors.exists) {
      //submit form
      setFormState('submitting');
      addUser({
        FullName: formData.firstName + ' ' + formData.lastName,
        Email: formData.email,
        Password: formData.password
      }).then(response => {
        if (response.data.success) {
          setFormState('success');
        } else {
          setErrors({ ...errors, form: response.data.message });
          setFormState('error');
        }
      }).catch(() => {
        setErrors({ ...errors, form: messages.errors.generic });
        setFormState('error');
      });
    }
  };

  if (formState == 'success') {
    return (
      <Container
        title="Account Created!"
        subtitle="Your account was created successfully!"
        className="signup-container"
      >
        <hr />
        <div className="email-message">
          <img src="/email-message.svg" alt="Email Message" />
        </div>
        <p>Please check your email inbox to activate your account before logging in.</p>
      </Container>
    );
  } else {
    return (
      <Container
        title="Sign Up for Collector"
        subtitle="Join our community of researchers"
        error={errors.form}
        className="signup-container"
      >
        <form className="signup-form" onSubmit={handleSubmit}>
          <div className="form-row">
            <Input
              label="First Name"
              name="firstName"
              value={formData.firstName}
              onInput={handleChange}
              required={true}
              maxLength={32}
              placeholder="Enter your first name"
              error={errors.firstName}
            ></Input>
            <Input
              label="Last Name"
              name="lastName"
              value={formData.lastName}
              onInput={handleChange}
              required={true}
              maxLength={32}
              placeholder="Enter your last name"
              error={errors.lastName}
            ></Input>
          </div>

          <Input
            label="Email"
            type="email"
            name="email"
            value={formData.email}
            onInput={handleChange}
            required={true}
            maxLength={64}
            placeholder="Enter your email"
            error={errors.email}
          ></Input>

          <Input
            label="Password"
            type="password"
            name="password"
            value={formData.password}
            onInput={handleChange}
            required={true}
            maxLength={16}
            placeholder="Create a password"
            error={errors.password}
          ></Input>

          <Input
            label="Confirm Password"
            type="password"
            name="confirmPassword"
            value={formData.confirmPassword}
            onInput={handleChange}
            required={true}
            maxLength={16}
            placeholder="Confirm your password"
            error={errors.confirmPassword}
          ></Input>
          {formState == 'submitting' ?
            <div className="submitting">Creating Account...</div>
            :
            <button type="submit" className="signup-button">
              Create Account
            </button>
          }
        </form>
      </Container>
    );
  }
}
