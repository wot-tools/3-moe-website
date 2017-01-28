class TanksController < ApplicationController

	def index
		@tanks = Tank.all.paginate(page: params[:page], per_page: 20)
	end
	
	def show
		@tank = Tank.find(params[:id])
		@marks = @tank.marks.paginate(page: params[:page], per_page: 2)
		rescue ActiveRecord::RecordNotFound
			redirect_to tanks_path and return
	end
end
