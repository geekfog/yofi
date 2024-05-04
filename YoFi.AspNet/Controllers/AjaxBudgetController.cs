﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Core.Repositories;

namespace YoFi.AspNet.Controllers
{
    /// <summary>
    /// Fulfill AJAX requests related to Payees
    /// </summary>
    [Route("ajax/budget")]
    [Produces("application/json")]
    [SkipStatusCodePages]
    public class AjaxBudgetController: Controller
    {
        /// <summary>
        /// Repository where we can take our actions
        /// </summary>
        private readonly IBudgetTxRepository _repository;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="repository"></param>
        public AjaxBudgetController(IBudgetTxRepository repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// Update the selection state for item #<paramref name="id"/>
        /// </summary>
        /// <param name="id">Which item</param>
        /// <param name="value">New selection state</param>
        [HttpPost("select/{id}")]
        [Authorize(Policy = "CanWrite")]
        [ValidateAntiForgeryToken]
        [ValidateBudgetTxExists]
        public async Task<IActionResult> Select(int id, bool value)
        {
            await _repository.SetSelectedAsync(id, value);

            return new OkResult();
        }
    }
}
